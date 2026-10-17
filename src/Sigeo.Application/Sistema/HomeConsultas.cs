using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

/// <summary>Leituras da tela inicial (Inicio/Home): o que o usuário tem a aprovar, ler e fazer, e os alertas da empresa.</summary>
public sealed class HomeConsultas(IFabricaDb fabrica, AprovacaoServico aprov)
{
    /// <summary>Carrega a home do usuário <paramref name="usuarioId"/> numa leitura só (as mensagens vêm do MensagensServico, à parte).</summary>
    public async Task<HomeDados> CarregarAsync(Guid? usuarioId, DateOnly hoje, int limiteMeus, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var agora = DateTimeOffset.UtcNow;
        var alcadas = await db.Alcadas.AsNoTracking().ToListAsync(ct);
        var abertas = await db.SolicitacoesAprovacao.AsNoTracking().Include(s => s.Etapas).Where(s => s.Status == StatusAprovacao.EmAprovacao).OrderBy(s => s.EnviadoEm).ToListAsync(ct);
        var aprovar = new List<PendenciaAprovacao>();
        if (usuarioId is not null)
            foreach (var s in abertas)
                if (await aprov.PodeDecidirAsync(db, s, ct))
                {
                    var horas = alcadas.FirstOrDefault(a => a.Entidade == s.Entidade && a.Ordem == s.EtapaAtual)?.PrazoHoras;
                    var desde = s.Etapas.Where(e => e.DecididoEm != null).Select(e => e.DecididoEm).Max() ?? s.EnviadoEm;
                    DateTimeOffset? prazo = horas is { } h ? desde.AddHours(h) : null;
                    aprovar.Add(new PendenciaAprovacao(s.Entidade, s.EntidadeId, s.Titulo, s.SolicitanteNome, s.Valor, s.EnviadoEm, prazo, prazo < agora));
                }
        var eu = usuarioId;
        var leituras = (await db.Ciencias.AsNoTracking().Include(c => c.Revisao).ThenInclude(r => r.Documento).Where(c => c.UsuarioId == eu && c.LidoEm == null).ToListAsync(ct))
            .Select(c => ($"{c.Revisao.Documento.Codigo} {c.Revisao.Rotulo} – {c.Revisao.Documento.Titulo}", c.Prazo)).ToList();
        var tarefas = await db.Tarefas.AsNoTracking().Include(t => t.Leituras).Where(t => t.ResponsavelId == eu && (t.Status == StatusTarefa.Aberta || t.Status == StatusTarefa.EmAndamento))
                                   .OrderBy(t => t.Prazo).ToListAsync(ct);
        var ha60 = agora.AddDays(-60);
        var meus = await db.SolicitacoesAprovacao.AsNoTracking().Include(s => s.Etapas).Where(s => s.SolicitanteUsuarioId == eu && s.EnviadoEm >= ha60).OrderByDescending(s => s.EnviadoEm).Take(limiteMeus).ToListAsync(ct);
        var del = await db.DelegacoesAprovacao.AsNoTracking().Where(d => d.TitularUsuarioId == eu && !d.Revogada && d.Inicio <= agora && d.Fim > agora).FirstOrDefaultAsync(ct);

        var p = await ParametrosServico.CarregarAsync(db, ct);
        var alertas = new List<(string Texto, int Qtd, string Tom, string Link)>();
        void A(int n, string texto, string tom, string link) { if (n > 0) alertas.Add((texto, n, tom, link)); }
        A(await db.FornecedorDocumentos.CountAsync(d => d.Bloqueia && d.DataValidade != null && d.DataValidade < hoje && d.Status != StatusDocumento.Reprovado, ct), "documento(s) de fornecedor vencido(s) — pedidos e medições bloqueados", "erro", "fornecedores/documentos");
        A(await db.FornecedorDocumentos.CountAsync(d => d.Status == StatusDocumento.EmValidacao, ct), "documento(s) enviados pelo portal aguardando validação", "aviso", "fornecedores/documentos");
        A(await db.Documentos.CountAsync(d => d.ProximaAnalise != null && d.ProximaAnalise < hoje, ct), "documento(s) do SGQ com análise crítica vencida", "erro", "sgq/documentos");
        A(await db.Copias.CountAsync(c => c.Status == StatusCopia.RecolhimentoPendente && c.PrazoRecolhimento < hoje, ct), "cópia(s) obsoleta(s) com recolhimento atrasado", "erro", "sgq/copias");
        A(await db.Grds.CountAsync(g => g.ConfirmadoEm == null && g.EnviadoEm < agora.AddDays(-1), ct), "GRD sem confirmação há mais de 1 dia", "aviso", "sgq/grd");
        A(await db.Fvss.CountAsync(f => f.Status == StatusFvs.Reprovada, ct), "FVS reprovada(s) aguardando reinspeção", "aviso", "obra/fvs");
        A(await db.Fvss.CountAsync(f => f.Status == StatusFvs.Aprovada, ct), "FVS aprovada(s) aguardando liberação do engenheiro", "info", "obra/fvs");
        A(await db.Rncs.CountAsync(r => (r.Status == StatusRnc.Aberta || r.Status == StatusRnc.EmTratamento) && r.Prazo < hoje, ct), "RNC vencida(s)", "erro", "obra/rnc");
        A(await db.AcoesCorretivas.CountAsync(a => a.Status == StatusAcaoCorretiva.AguardandoEficacia && a.DataEficacia <= hoje, ct), "ação(ões) corretiva(s) com eficácia a verificar", "aviso", "obra/acoes");
        A((await db.Equipamentos.AsNoTracking().ToListAsync(ct)).Count(e => e.Status(hoje, p.Int(Parametros.AlertaCalibracaoDias)) is StatusEquipamento.VenceEmBreve or StatusEquipamento.Bloqueado), "equipamento(s) de medição vencendo ou bloqueados", "aviso", "obra/equipamentos");
        A(await db.EnsaiosRecebimento.CountAsync(x => x.Situacao == SituacaoEnsaio.Aguardando && x.DataPrevista < hoje, ct), "ensaio(s) de recebimento com resultado atrasado", "erro", "recebimentos");
        A(await db.Tarefas.CountAsync(t => (t.Status == StatusTarefa.Aberta || t.Status == StatusTarefa.EmAndamento) && t.Prazo < hoje, ct), "tarefa(s) atrasada(s) na empresa", "erro", "tarefas");
        A(await db.PlanosAcao.CountAsync(x => x.Status != StatusPlanoAcao.Concluida && x.Prazo < hoje, ct), "plano(s) de ação de fornecedor vencido(s)", "erro", "fornecedores/planos");
        A(await db.Contratos.CountAsync(k => k.Status == StatusContrato.Ativo && k.VigenciaFim <= hoje.AddDays(30), ct), "contrato(s) vencendo em 30 dias", "aviso", "contratos");
        A(await db.Colaboradores.CountAsync(c => c.Situacao == SituacaoColaborador.Bloqueado && (c.DataSaida == null || c.DataSaida > hoje), ct), "colaborador(es) bloqueados (ASO/integração)", "erro", "sst/colaboradores");
        A(await db.DocumentosColaborador.CountAsync(d => d.Validacao == ValidacaoDoc.Aguardando, ct), "documento(s) de colaborador aguardando validação do SST", "aviso", "sst/documentos");
        A(await db.PermissoesTrabalho.CountAsync(x => x.Status == StatusPermissaoTrabalho.Liberada && x.Fim < agora, ct), "PT sem encerramento", "erro", "sst/pt");
        A(await db.Epis.CountAsync(e => e.Ativo && e.ValidadeCa <= hoje.AddDays(p.Int(Parametros.AlertaCaDias)), ct), "EPI com CA vencendo/vencido", "aviso", "sst/epi");
        var vazio = !await db.Obras.AnyAsync(ct) || !await db.Fornecedores.AnyAsync(ct);
        return new(aprovar, leituras, tarefas, meus, del, alertas, vazio);
    }
}
