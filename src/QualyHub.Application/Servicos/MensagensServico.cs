using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Comum;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Colaboracao;
using QualyHub.Domain.Common;

namespace QualyHub.Application.Servicos;

/// <summary>
/// Caixa de mensagens interna (só entre usuários do QualyHub). Cada pessoa vê apenas as conversas de que participa.
/// Conversa pode ficar ligada a um registro (RNC, pedido, contrato, versão do cronograma…) e aparece dentro dele.
/// Nada é apagado; "lido por quem e quando" fica registrado por mensagem. Aviso por e-mail segue a regra
/// "Nova mensagem interna" (Sistema › Parâmetros e Notificações).
/// </summary>
public sealed class MensagensServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio, CanalAvisos canal)
{
    public enum Pasta { Entrada, Enviadas, Arquivadas, Registros }

    /// <summary>Destinatário escolhível: pessoa ("u:id"), perfil ("p:Engenharia") ou obra ("o:id" = todos com acesso à obra).</summary>
    public sealed record Destinatario(string Chave, string Texto, string Grupo);

    public sealed record NovaMensagem(string Assunto, string Texto, IReadOnlyList<string> Para, IReadOnlyList<string>? Copia = null,
        Guid? ObraId = null, string? RegistroEntidade = null, Guid? RegistroId = null);

    public sealed record ResumoConversa(Guid Id, int Numero, string Assunto, string? RegistroEntidade, Guid? RegistroId, string? RegistroDescricao,
        DateTimeOffset UltimaEm, string UltimoAutor, string Previa, int NaoLidas, string Participantes, bool Arquivada, bool EuAbri);

    public sealed record LeituraVista(string Nome, DateTimeOffset LidaEm);
    public sealed record MensagemVista(Guid Id, Guid AutorId, string Autor, string Html, DateTimeOffset EnviadaEm, IReadOnlyList<LeituraVista> Lidos, IReadOnlyList<string> NaoLeram);
    public sealed record ParticipanteVisto(Guid UsuarioId, string Nome, PapelParticipante Papel);
    public sealed record ConversaVista(Guid Id, int Numero, string Assunto, string? RegistroEntidade, Guid? RegistroId, string? RegistroDescricao, string? RegistroLink,
        IReadOnlyList<ParticipanteVisto> Participantes, IReadOnlyList<MensagemVista> Mensagens, bool Arquivada);

    private Guid Eu => usuario.UsuarioId ?? throw new RegraDeNegocioException("Mensagens só para usuários do sistema.");

    // ───────── Destinatários ─────────

    public async Task<List<Destinatario>> DestinatariosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var eu = usuario.UsuarioId;
        var pessoas = await db.Usuarios.Where(u => u.Ativo && u.Id != eu).OrderBy(u => u.Nome)
            .Select(u => new { u.Id, u.Nome, u.Perfil, u.Funcao }).ToListAsync(ct);
        var obras = (await db.Obras.OrderBy(o => o.Codigo).Select(o => new { o.Id, o.Codigo, o.Nome }).ToListAsync(ct))
            .Where(o => usuario.AcessoTodasObras || usuario.ObrasPermitidas.Contains(o.Id)).ToList();
        var r = new List<Destinatario>();
        r.AddRange(obras.Select(o => new Destinatario($"o:{o.Id}", $"Todos da obra {o.Codigo} – {o.Nome}", "Grupos por obra")));
        r.AddRange(pessoas.Select(p => p.Perfil).Distinct().OrderBy(p => p.ToString()).Select(p => new Destinatario($"p:{p}", $"Perfil {Rotulo(p)}", "Grupos por perfil")));
        r.AddRange(pessoas.Select(p => new Destinatario($"u:{p.Id}", p.Funcao is { Length: > 0 } f ? $"{p.Nome} · {f}" : $"{p.Nome} · {Rotulo(p.Perfil)}", "Pessoas")));
        return r;
    }

    private static string Rotulo(PerfilUsuario p) => p switch
    {
        PerfilUsuario.Engenharia => "Engenharia", PerfilUsuario.Diretoria => "Diretoria", PerfilUsuario.Qualidade => "Qualidade",
        PerfilUsuario.Suprimentos => "Suprimentos", PerfilUsuario.Financeiro => "Financeiro", PerfilUsuario.Campo => "Campo",
        PerfilUsuario.Almoxarifado => "Almoxarifado", PerfilUsuario.Seguranca => "Segurança", PerfilUsuario.Terceiro => "Terceiro",
        PerfilUsuario.Administrador => "Administrador", _ => p.ToString()
    };

    /// <summary>Transforma as escolhas (pessoas e grupos) em pessoas ativas. Grupo de obra = quem tem acesso a ela.</summary>
    private static async Task<List<(Guid Id, string Nome)>> ResolverAsync(IQualyHubDb db, IEnumerable<string> chaves, CancellationToken ct)
    {
        var lista = chaves.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (lista.Count == 0) return [];
        var usuarios = await db.Usuarios.AsNoTracking().Include(u => u.Obras).Where(u => u.Ativo).ToListAsync(ct);
        var r = new List<(Guid, string)>();
        foreach (var c in lista)
        {
            var (tipo, valor) = (c[..Math.Min(2, c.Length)], c.Length > 2 ? c[2..] : "");
            IEnumerable<Domain.Estrutura.Usuario> q = tipo switch
            {
                "u:" when Guid.TryParse(valor, out var id) => usuarios.Where(u => u.Id == id),
                "p:" when Enum.TryParse<PerfilUsuario>(valor, out var p) => usuarios.Where(u => u.Perfil == p),
                "o:" when Guid.TryParse(valor, out var o) => usuarios.Where(u => u.AcessoTodasObras || u.Obras.Any(x => x.ObraId == o)),
                _ => throw new RegraDeNegocioException("Destinatário inválido.")
            };
            r.AddRange(q.Select(u => (u.Id, u.Nome)));
        }
        return r.DistinctBy(x => x.Item1).ToList();
    }

    // ───────── Enviar e responder ─────────

    public async Task<Guid> EnviarAsync(NovaMensagem m, CancellationToken ct = default)
    {
        var eu = Eu;
        await using var db = fabrica.Criar();
        var para = await ResolverAsync(db, m.Para, ct);
        var copia = (await ResolverAsync(db, m.Copia ?? [], ct)).Where(c => para.All(p => p.Id != c.Id)).ToList();
        var agora = relogio.GetUtcNow();
        var c = Conversa.Abrir(m.Assunto, m.Texto, eu, usuario.Nome, para, copia, agora);
        c.Numero = await Numerador.ProximoAsync(db, "Conversa", ct);
        c.ObraId = m.ObraId;
        if (m.RegistroEntidade is { Length: > 0 } ent && m.RegistroId is { } rid)
        {
            var (desc, obra) = await DescreverRegistroAsync(db, ent, rid, ct);
            c.LigarRegistro(ent, rid, desc);
            c.ObraId ??= obra;
        }
        if (c.ObraId is { } oid && !usuario.AcessoTodasObras && !usuario.ObrasPermitidas.Contains(oid))
            throw new RegraDeNegocioException("Você não tem acesso a essa obra.");
        db.Conversas.Add(c);
        var outros = c.Participantes.Where(p => p.UsuarioId != eu).Select(p => p.UsuarioId).ToList();
        await AvisarAsync(db, c, c.Mensagens.First(), outros, ct);
        await db.SaveChangesAsync(ct);
        canal.Publicar(usuario.EmpresaId, outros);
        return c.Id;
    }

    public async Task ResponderAsync(Guid conversaId, string texto, IReadOnlyList<string>? incluir = null, CancellationToken ct = default)
    {
        var eu = Eu;
        await using var db = fabrica.Criar();
        var c = await db.Conversas.Include(x => x.Participantes).FirstOrDefaultAsync(x => x.Id == conversaId, ct)
                ?? throw new RegraDeNegocioException("Conversa não encontrada.");
        var agora = relogio.GetUtcNow();
        var m = c.Responder(eu, usuario.Nome, texto, agora);
        db.MensagensConversa.Add(m);
        foreach (var (id, nome) in await ResolverAsync(db, incluir ?? [], ct))
            if (c.Incluir(id, nome, PapelParticipante.Copia) is { } novo) db.ParticipantesConversa.Add(novo);
        if (c.Participantes.Count > Conversa.MaxParticipantes) throw new RegraDeNegocioException($"Máximo de {Conversa.MaxParticipantes} participantes por conversa.");
        var outros = c.Participantes.Where(p => p.UsuarioId != eu).Select(p => p.UsuarioId).ToList();
        await AvisarAsync(db, c, m, outros, ct);
        await db.SaveChangesAsync(ct);
        canal.Publicar(usuario.EmpresaId, outros);
    }

    private async Task AvisarAsync(IQualyHubDb db, Conversa c, MensagemConversa m, IReadOnlyList<Guid> para, CancellationToken ct) =>
        await AvisosServico.EnfileirarAsync(db, EventosAviso.NovaMensagem, para, $"msg:{m.Id:N}",
            new Dictionary<string, string?>
            {
                ["remetente"] = usuario.Nome, ["assunto"] = $"{usuario.Nome}: {c.Assunto}", ["titulo"] = c.Assunto,
                ["registro"] = c.RegistroDescricao, ["mensagem"] = TextoFormatado.Resumo(m.Texto, 300)
            }, $"mensagens?id={c.Id}", ct);

    /// <summary>Descrição do registro ligado (ex.: "RNC 0012") e a obra dele, para gravar na conversa.</summary>
    private static async Task<(string Descricao, Guid? Obra)> DescreverRegistroAsync(IQualyHubDb db, string ent, Guid id, CancellationToken ct)
    {
        var rot = RotasRegistro.Rotulo(ent);
        (string? num, Guid? obra) r = ent switch
        {
            "Rnc" => await db.Rncs.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero, x.ObraId)).FirstOrDefaultAsync(ct),
            "AcaoCorretiva" => await db.AcoesCorretivas.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero, null)).FirstOrDefaultAsync(ct),
            "Auditoria" => await db.AuditoriasInternas.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Codigo, null)).FirstOrDefaultAsync(ct),
            "Fvs" => await db.Fvss.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero, x.ObraId)).FirstOrDefaultAsync(ct),
            "PlanoAcao" => await db.PlanosAcao.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero, x.ObraId)).FirstOrDefaultAsync(ct),
            "Requisicao" => await db.Requisicoes.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Cotacao" => await db.Cotacoes.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Pedido" => await db.Pedidos.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Contrato" => await db.Contratos.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero, x.ObraId)).FirstOrDefaultAsync(ct),
            "Medicao" => await db.Medicoes.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.NumeroMedicao.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Recebimento" => await db.Recebimentos.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Pagamento" => await db.Pagamentos.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Tarefa" => await db.Tarefas.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            "Cronograma" => await db.VersoesCronograma.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Codigo, x.ObraId)).FirstOrDefaultAsync(ct),
            "Restricao" => await db.Restricoes.Where(x => x.Id == id).Select(x => new ValueTuple<string?, Guid?>(x.Numero.ToString(), x.ObraId)).FirstOrDefaultAsync(ct),
            _ => throw new RegraDeNegocioException("Tipo de registro não pode receber mensagens.")
        };
        if (r.num is null) throw new RegraDeNegocioException("Registro não encontrado (ou sem acesso).");
        return ($"{rot} {r.num}".Trim(), r.obra);
    }

    // ───────── Caixa ─────────

    public async Task<List<ResumoConversa>> CaixaAsync(Pasta pasta, string? busca = null, int limite = 200, CancellationToken ct = default)
    {
        var eu = Eu;
        await using var db = fabrica.CriarLeitura();
        var minhas = db.ParticipantesConversa.Where(p => p.UsuarioId == eu);
        minhas = pasta switch
        {
            Pasta.Entrada => minhas.Where(p => !p.Arquivada && db.MensagensConversa.Any(m => m.ConversaId == p.ConversaId && m.AutorId != eu)),
            Pasta.Enviadas => minhas.Where(p => db.MensagensConversa.Any(m => m.ConversaId == p.ConversaId && m.AutorId == eu)),
            Pasta.Arquivadas => minhas.Where(p => p.Arquivada),
            _ => minhas
        };
        var q = from p in minhas join c in db.Conversas on p.ConversaId equals c.Id select new { p, c };
        if (pasta == Pasta.Registros) q = q.Where(x => x.c.RegistroId != null);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            q = q.Where(x => x.c.Assunto.Contains(b) || (x.c.RegistroDescricao != null && x.c.RegistroDescricao.Contains(b)) || x.c.RemetenteNome.Contains(b)
                             || db.MensagensConversa.Any(m => m.ConversaId == x.c.Id && (m.Texto.Contains(b) || m.AutorNome.Contains(b))));
        }
        var linhas = await q.OrderByDescending(x => x.c.UltimaEm).Take(limite).Select(x => new
        {
            x.c.Id, x.c.Numero, x.c.Assunto, x.c.RegistroEntidade, x.c.RegistroId, x.c.RegistroDescricao, x.c.UltimaEm, x.c.RemetenteId,
            x.p.Arquivada, x.p.LidaAte,
            Ultima = db.MensagensConversa.Where(m => m.ConversaId == x.c.Id).OrderByDescending(m => m.EnviadaEm).Select(m => new { m.AutorNome, m.Texto }).First(),
            NaoLidas = db.MensagensConversa.Count(m => m.ConversaId == x.c.Id && m.AutorId != eu && (x.p.LidaAte == null || m.EnviadaEm > x.p.LidaAte)),
            Nomes = db.ParticipantesConversa.Where(o => o.ConversaId == x.c.Id && o.UsuarioId != eu).OrderBy(o => o.Papel).Select(o => o.Nome).Take(6).ToList()
        }).ToListAsync(ct);
        return linhas.Select(l => new ResumoConversa(l.Id, l.Numero, l.Assunto, l.RegistroEntidade, l.RegistroId, l.RegistroDescricao, l.UltimaEm,
            l.Ultima.AutorNome, TextoFormatado.Resumo(l.Ultima.Texto, 110), l.NaoLidas, string.Join(", ", l.Nomes), l.Arquivada, l.RemetenteId == eu)).ToList();
    }

    /// <summary>Quantas conversas têm mensagem não lida (contador do menu).</summary>
    public async Task<int> NaoLidasAsync(CancellationToken ct = default)
    {
        if (usuario.UsuarioId is not { } eu) return 0;
        await using var db = fabrica.CriarLeitura();
        return await db.ParticipantesConversa.CountAsync(p => p.UsuarioId == eu &&
            db.MensagensConversa.Any(m => m.ConversaId == p.ConversaId && m.AutorId != eu && (p.LidaAte == null || m.EnviadaEm > p.LidaAte)), ct);
    }

    /// <summary>Abre a conversa e registra a leitura (primeira leitura de cada mensagem).</summary>
    public async Task<ConversaVista> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        var eu = Eu;
        await using var db = fabrica.Criar();
        var c = await db.Conversas.Include(x => x.Participantes).Include(x => x.Mensagens).ThenInclude(m => m.Leituras).AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new RegraDeNegocioException("Conversa não encontrada.");
        var eu_p = c.Participantes.FirstOrDefault(p => p.UsuarioId == eu) ?? throw new RegraDeNegocioException("Você não participa desta conversa.");
        var agora = relogio.GetUtcNow();
        var marcou = false;
        foreach (var m in c.Mensagens.Where(m => m.AutorId != eu && m.Leituras.All(l => l.UsuarioId != eu)))
        {
            var l = new LeituraMensagem { MensagemId = m.Id, UsuarioId = eu, Nome = usuario.Nome, LidaEm = agora };
            m.Leituras.Add(l); db.LeiturasMensagem.Add(l); marcou = true;
        }
        if (eu_p.LidaAte is null || eu_p.LidaAte < c.UltimaEm) { eu_p.LidaAte = agora; marcou = true; }
        if (marcou)
        {
            await db.SaveChangesAsync(ct);
            canal.Publicar(usuario.EmpresaId, [eu], acordarEnvio: false);
        }
        return Vista(c, eu_p.Arquivada);
    }

    private static ConversaVista Vista(Conversa c, bool arquivada)
    {
        var participantes = c.Participantes.OrderBy(p => p.Papel).ThenBy(p => p.Nome).Select(p => new ParticipanteVisto(p.UsuarioId, p.Nome, p.Papel)).ToList();
        var msgs = c.Mensagens.OrderBy(m => m.EnviadaEm).Select(m =>
        {
            var lidos = m.Leituras.OrderBy(l => l.LidaEm).Select(l => new LeituraVista(l.Nome, l.LidaEm)).ToList();
            var faltam = c.Participantes.Where(p => p.UsuarioId != m.AutorId && m.Leituras.All(l => l.UsuarioId != p.UsuarioId)).Select(p => p.Nome).ToList();
            return new MensagemVista(m.Id, m.AutorId, m.AutorNome, TextoFormatado.ParaHtml(m.Texto), m.EnviadaEm, lidos, faltam);
        }).ToList();
        return new ConversaVista(c.Id, c.Numero, c.Assunto, c.RegistroEntidade, c.RegistroId, c.RegistroDescricao,
            RotasRegistro.Link(c.RegistroEntidade, c.RegistroId), participantes, msgs, arquivada);
    }

    public async Task ArquivarAsync(Guid id, bool arquivar, CancellationToken ct = default)
    {
        var eu = Eu;
        await using var db = fabrica.Criar();
        var p = await db.ParticipantesConversa.FirstOrDefaultAsync(x => x.ConversaId == id && x.UsuarioId == eu, ct)
                ?? throw new RegraDeNegocioException("Você não participa desta conversa.");
        p.Arquivada = arquivar;
        await db.SaveChangesAsync(ct);
        canal.Publicar(usuario.EmpresaId, [eu], acordarEnvio: false);
    }

    /// <summary>Conversas ligadas ao registro de que o usuário participa (quadro "Mensagens" dentro do registro).</summary>
    public async Task<List<ResumoConversa>> DoRegistroAsync(string entidade, Guid registroId, CancellationToken ct = default)
    {
        if (usuario.UsuarioId is not { } eu) return [];
        await using var db = fabrica.CriarLeitura();
        var ids = await (from p in db.ParticipantesConversa
                         join c in db.Conversas on p.ConversaId equals c.Id
                         where p.UsuarioId == eu && c.RegistroEntidade == entidade && c.RegistroId == registroId
                         select c.Id).ToListAsync(ct);
        if (ids.Count == 0) return [];
        return (await CaixaAsync(Pasta.Registros, null, 500, ct)).Where(r => ids.Contains(r.Id)).ToList();
    }
}
