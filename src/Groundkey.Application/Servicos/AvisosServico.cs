using Microsoft.EntityFrameworkCore;
using Groundkey.Application.Comum;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Colaboracao;
using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Planejamento;
using Groundkey.Domain.Qualidade;

namespace Groundkey.Application.Servicos;

/// <summary>
/// Avisos por e-mail a partir das regras de notificação (Sistema › Parâmetros e Notificações).
/// Cada aviso entra numa fila (tabela AvisosEmail) junto com o fato que o gerou; a rotina de envio manda e tenta de novo
/// se falhar. A regra decide se o evento manda e-mail (canal E-mail ligado e regra ativa) e o texto ({campos}).
/// A chave do aviso impede repetir o mesmo lembrete (ex.: aprovação pendente: uma vez por dia por pessoa).
/// </summary>
public static class AvisosServico
{
    public sealed record Destino(Guid UsuarioId, string Nome, string Email);

    /// <summary>Põe na fila um aviso do evento para cada pessoa (se a regra mandar e-mail). Grava junto com o próximo SaveChanges.</summary>
    public static async Task<int> EnfileirarAsync(IGroundkeyDb db, string evento, IEnumerable<Guid> usuarios, string chave,
        IReadOnlyDictionary<string, string?> valores, string? link, CancellationToken ct = default)
    {
        var regra = await RegraAsync(db, evento, ct);
        if (regra is null) return 0;
        var ids = usuarios.Where(u => u != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return 0;
        var destinos = await db.Usuarios.AsNoTracking().Where(u => ids.Contains(u.Id) && u.Ativo && u.Email != "")
            .Select(u => new Destino(u.Id, u.Nome, u.Email)).ToListAsync(ct);
        return await EnfileirarAsync(db, regra, destinos, chave, valores, link, ct);
    }

    private static async Task<RegraNotificacao?> RegraAsync(IGroundkeyDb db, string evento, CancellationToken ct)
    {
        var r = await db.RegrasNotificacao.AsNoTracking().FirstOrDefaultAsync(x => x.Evento == evento && x.Ativa, ct);
        return r is not null && r.Canal.HasFlag(CanalNotificacao.Email) ? r : null;
    }

    private static async Task<int> EnfileirarAsync(IGroundkeyDb db, RegraNotificacao regra, IReadOnlyList<Destino> destinos, string chave,
        IReadOnlyDictionary<string, string?> valores, string? link, CancellationToken ct)
    {
        if (destinos.Count == 0) return 0;
        var chaves = destinos.ToDictionary(d => d.UsuarioId, d => Limitar($"{chave}:{d.UsuarioId:N}", 200));
        var lista = chaves.Values.ToList();
        var existentes = (await db.AvisosEmail.AsNoTracking().Where(a => lista.Contains(a.Chave)).Select(a => a.Chave).ToListAsync(ct))
            .Concat(db.AvisosEmail.Local.Select(a => a.Chave)).ToHashSet();
        var n = 0;
        foreach (var d in destinos)
        {
            var k = chaves[d.UsuarioId];
            if (existentes.Contains(k)) continue;
            var v = new Dictionary<string, string?>(valores, StringComparer.OrdinalIgnoreCase) { ["nome"] = d.Nome.Split(' ')[0], ["link"] = "{link}" };
            var texto = AvisoEmail.Preencher(regra.ModeloMensagem, v);
            var assunto = valores.TryGetValue("assunto", out var a) && !string.IsNullOrWhiteSpace(a) ? a! : regra.Evento;
            db.AvisosEmail.Add(new AvisoEmail
            {
                Evento = regra.Evento, Chave = k, UsuarioId = d.UsuarioId, Para = d.Email, NomeDestinatario = d.Nome,
                Assunto = Limitar($"Groundkey · {assunto}", 250), Texto = texto, Link = link
            });
            n++;
        }
        return n;
    }

    private static string Limitar(string s, int max) => s.Length <= max ? s : s[..max];

    // ───────── Varredura periódica (lembretes e prazos) ─────────

    /// <summary>
    /// Gera os avisos que dependem de data ou de situação: aprovação pendente (1×/dia), prazo de aprovação estourado,
    /// leitura obrigatória nova, tarefa atribuída e atrasada, RNC vencida e restrição do planejamento vencendo.
    /// Idempotente (chave por dia/registro): pode rodar quantas vezes quiser.
    /// </summary>
    public static async Task<int> GerarAsync(IGroundkeyDb db, DateTimeOffset agora, DateOnly hoje, CancellationToken ct = default)
    {
        var regras = (await db.RegrasNotificacao.AsNoTracking().Where(r => r.Ativa).ToListAsync(ct))
            .Where(r => r.Canal.HasFlag(CanalNotificacao.Email)).GroupBy(r => r.Evento).ToDictionary(g => g.Key, g => g.First());
        if (regras.Count == 0) return 0;
        var usuarios = await db.Usuarios.AsNoTracking().Include(u => u.Obras).Where(u => u.Ativo).ToListAsync(ct);
        var porId = usuarios.ToDictionary(u => u.Id);
        Destino? D(Guid? id) => id is { } i && porId.TryGetValue(i, out var u) && !string.IsNullOrWhiteSpace(u.Email) ? new Destino(u.Id, u.Nome, u.Email) : null;
        List<Destino> Ds(IEnumerable<Guid?> ids) => ids.Select(D).Where(x => x is not null).Select(x => x!).DistinctBy(x => x.UsuarioId).ToList();
        var dia = hoje.ToString("yyyyMMdd");
        var recente = agora.AddDays(-3); // avisos de "novo" só para o que é novo: ligar a regra não dispara e-mail do passado
        var n = 0;

        // Aprovações em andamento: quem pode decidir a etapa atual (nominal, perfil com acesso à obra, substituto por delegação).
        if (regras.TryGetValue(EventosAviso.AprovacaoPendente, out var rAprov) | regras.TryGetValue(EventosAviso.PrazoAprovacaoEstourado, out var rPrazo))
        {
            var abertas = await db.SolicitacoesAprovacao.AsNoTracking().Include(s => s.Etapas).Where(s => s.Status == StatusAprovacao.EmAprovacao).ToListAsync(ct);
            var alcadas = await db.Alcadas.AsNoTracking().ToListAsync(ct);
            var delegacoes = await db.DelegacoesAprovacao.AsNoTracking().Where(d => !d.Revogada && d.Inicio <= agora && d.Fim > agora).ToListAsync(ct);
            foreach (var s in abertas)
            {
                if (s.Pendente is not { } etapa) continue;
                var aprovadores = AprovadoresDaEtapa(etapa, s, usuarios);
                var substitutos = delegacoes.Where(d => aprovadores.Contains(d.TitularUsuarioId) && d.Vigente(agora, s.Entidade, s.ObraId)).Select(d => d.SubstitutoUsuarioId);
                var destinos = Ds(aprovadores.Concat(substitutos).Where(u => u != s.SolicitanteUsuarioId).Select(u => (Guid?)u));
                var valores = new Dictionary<string, string?> { ["documento"] = s.Titulo, ["assunto"] = $"Aprovação pendente: {s.Titulo}", ["solicitante"] = s.SolicitanteNome };
                var link = RotasRegistro.Link(s.Entidade, s.EntidadeId);
                if (rAprov is not null) n += await EnfileirarAsync(db, rAprov, destinos, $"aprov:{etapa.Id:N}:{dia}", valores, link, ct);
                var desde = s.Etapas.Where(e => e.DecididoEm != null).Select(e => e.DecididoEm).Max() ?? s.EnviadoEm;
                var horas = alcadas.FirstOrDefault(a => a.Entidade == s.Entidade && a.Ordem == s.EtapaAtual)?.PrazoHoras;
                if (rPrazo is not null && horas is { } h && desde.AddHours(h) < agora)
                    n += await EnfileirarAsync(db, rPrazo, destinos, $"prazo:{etapa.Id:N}", new Dictionary<string, string?>(valores) { ["assunto"] = $"Aprovação atrasada: {s.Titulo}" }, link, ct);
            }
        }

        if (regras.TryGetValue(EventosAviso.NovaRevisao, out var rRev))
        {
            var ciencias = await db.Ciencias.AsNoTracking().Include(c => c.Revisao).ThenInclude(r => r.Documento)
                .Where(c => c.LidoEm == null && c.Prazo >= hoje && c.CriadoEm >= recente).Take(500).ToListAsync(ct);
            foreach (var c in ciencias)
            {
                var doc = $"{c.Revisao.Documento.Codigo} – {c.Revisao.Documento.Titulo}";
                n += await EnfileirarAsync(db, rRev, Ds([c.UsuarioId]), $"ciencia:{c.Id:N}",
                    new Dictionary<string, string?> { ["documento"] = doc, ["revisao"] = c.Revisao.Rotulo, ["data"] = c.Prazo.ToString("dd/MM/yyyy"), ["assunto"] = $"Leitura obrigatória: {doc}" },
                    "sgq/leituras", ct);
            }
        }

        var abertasT = await db.Tarefas.AsNoTracking().Where(t => t.Status == StatusTarefa.Aberta || t.Status == StatusTarefa.EmAndamento).Take(2000).ToListAsync(ct);
        if (regras.TryGetValue(EventosAviso.TarefaAtribuida, out var rTar))
            foreach (var t in abertasT.Where(t => t.ResponsavelNome != t.CriadoPor && t.CriadoEm >= recente))
                n += await EnfileirarAsync(db, rTar, Ds([t.ResponsavelId]), $"tarefa:{t.Id:N}:{t.ResponsavelId:N}",
                    new Dictionary<string, string?> { ["tarefa"] = $"{t.Numero} – {t.Titulo}", ["solicitante"] = t.CriadoPor, ["data"] = t.Prazo.ToString("dd/MM/yyyy"), ["assunto"] = $"Nova tarefa: {t.Titulo}" },
                    $"tarefas?id={t.Id}", ct);
        if (regras.TryGetValue(EventosAviso.TarefaAtrasada, out var rAtr))
            foreach (var t in abertasT.Where(t => t.Atrasada(hoje)))
                n += await EnfileirarAsync(db, rAtr, Ds([t.ResponsavelId]), $"atrasada:{t.Id:N}:{dia}",
                    new Dictionary<string, string?> { ["tarefa"] = $"{t.Numero} – {t.Titulo}", ["data"] = t.Prazo.ToString("dd/MM/yyyy"), ["assunto"] = $"Tarefa atrasada: {t.Titulo}" },
                    $"tarefas?id={t.Id}", ct);

        if (regras.TryGetValue(EventosAviso.RncVencida, out var rRnc))
        {
            var rncs = await db.Rncs.AsNoTracking().Where(r => (r.Status == StatusRnc.Aberta || r.Status == StatusRnc.EmTratamento) && r.Prazo < hoje && r.ResponsavelId != null).Take(500).ToListAsync(ct);
            foreach (var r in rncs)
                n += await EnfileirarAsync(db, rRnc, Ds([r.ResponsavelId]), $"rnc:{r.Id:N}:{dia}",
                    new Dictionary<string, string?> { ["rnc"] = $"RNC {r.Numero}", ["data"] = r.Prazo.ToString("dd/MM/yyyy"), ["assunto"] = $"RNC {r.Numero} vencida" },
                    $"obra/rnc?id={r.Id}", ct);
        }

        if (regras.TryGetValue(EventosAviso.RestricaoVencendo, out var rRes))
        {
            var limite = hoje.AddDays(2);
            var restricoes = await db.Restricoes.AsNoTracking().Where(r => r.Status == StatusRestricao.Aberta && r.DataLimite <= limite && r.ResponsavelId != null).Take(500).ToListAsync(ct);
            foreach (var r in restricoes)
                n += await EnfileirarAsync(db, rRes, Ds([r.ResponsavelId]), $"restricao:{r.Id:N}:{dia}",
                    new Dictionary<string, string?> { ["restricao"] = $"{r.Numero} – {r.Descricao}", ["atividade"] = r.AtividadeNome, ["data"] = r.DataLimite.ToString("dd/MM/yyyy"),
                        ["assunto"] = r.DataLimite < hoje ? $"Restrição vencida: {r.AtividadeNome}" : $"Restrição vence em {r.DataLimite:dd/MM}: {r.AtividadeNome}" },
                    $"engenharia/last-planner?obra={r.ObraId}&aba=medio&restricao={r.Id}", ct);
        }
        return n;
    }

    /// <summary>Quem pode decidir a etapa: o aprovador nominal; sem ele, usuários ativos do perfil com acesso à obra do documento.</summary>
    public static List<Guid> AprovadoresDaEtapa(EtapaAprovacao etapa, SolicitacaoAprovacao s, IEnumerable<Usuario> usuarios)
    {
        if (etapa.UsuarioAprovadorId is { } u) return [u];
        return usuarios.Where(x => x.Ativo && x.Perfil == etapa.PerfilAprovador && (x.VeTodasObras || s.ObraId is null || x.Obras.Any(o => o.ObraId == s.ObraId)))
                       .Select(x => x.Id).ToList();
    }

    // ───────── Envio ─────────

    /// <summary>Manda os avisos da fila (até <paramref name="lote"/> por vez). Sem e-mail configurado, nada sai e a fila espera.</summary>
    public static async Task<(int Enviados, int Falhas)> EnviarPendentesAsync(IGroundkeyDb db, IEnvioMensagens envio, string enderecoSite, string construtora,
        DateTimeOffset agora, CancellationToken ct = default, int lote = 50)
    {
        if (!envio.EmailDisponivel) return (0, 0);
        var fila = await db.AvisosEmail.Where(a => a.Situacao == SituacaoAviso.Pendente && (a.ProximaTentativaEm == null || a.ProximaTentativaEm <= agora))
            .OrderBy(a => a.CriadoEm).Take(lote).ToListAsync(ct);
        int ok = 0, falhas = 0;
        foreach (var a in fila)
        {
            var url = string.IsNullOrWhiteSpace(a.Link) ? enderecoSite : $"{enderecoSite.TrimEnd('/')}/{a.Link.TrimStart('/')}";
            var texto = a.Texto.Replace("{link}", url);
            try
            {
                await envio.EnviarEmailAsync(a.Para, a.Assunto, $"{texto}\n\n{url}\n\n— {construtora} · Groundkey", Html(a.NomeDestinatario, texto, url, construtora), ct);
                a.MarcarEnviado(agora);
                ok++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                a.MarcarFalha(ex.Message, agora);
                falhas++;
            }
            await db.SaveChangesAsync(ct); // um por um: o que já saiu não sai de novo se o próximo derrubar o processo
        }
        return (ok, falhas);
    }

    private static string Html(string nome, string texto, string url, string construtora)
    {
        static string E(string s) => System.Net.WebUtility.HtmlEncode(s);
        var corpo = string.Join("", texto.Replace(url, "").Split('\n').Select(l => $"<p style=\"margin:0 0 8px\">{E(l)}</p>"));
        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937;max-width:560px">
              <div style="background:#2f3a4b;color:#fff;padding:12px 16px;border-radius:8px 8px 0 0;font-weight:700;letter-spacing:.08em">QUALYHUB</div>
              <div style="border:1px solid #e2e6ec;border-top:0;padding:16px;border-radius:0 0 8px 8px">
                <p style="margin:0 0 12px">Olá, {E(nome.Split(' ')[0])}.</p>
                {corpo}
                <p style="margin:16px 0"><a href="{E(url)}" style="background:#43597a;color:#fff;padding:9px 16px;border-radius:999px;text-decoration:none;font-weight:600">Abrir no Groundkey</a></p>
                <p style="margin:0;color:#7b8594;font-size:12px">{E(construtora)} · aviso automático, não responda este e-mail. Para mudar os avisos, fale com o administrador (Sistema › Parâmetros e Notificações).</p>
              </div>
            </div>
            """;
    }
}
