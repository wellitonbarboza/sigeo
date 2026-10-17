using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

// Inicio
public sealed partial class PortalServico
{
    public async Task<InicioPortal> InicioAsync(string token, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var agora = relogio.GetUtcNow();
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var pend = new List<PendenciaPortal>();
        foreach (var d in s.Documentos.Where(d => !d.PorEntrega))
        {
            if (d.Situacao == StatusDocumento.Reprovado) pend.Add(new(1, "Recusado", "erro", d.Nome, d.Detalhe ?? "", "empresa", d.TipoId));
            else if (d.Situacao == StatusDocumento.Vencido) pend.Add(new(1, "Vencido", "erro", d.Nome, $"Venceu em {d.Validade:dd/MM/yyyy}", "empresa", d.TipoId));
            else if (d.Situacao == StatusDocumento.PendenteEnvio && d.Obrigatorio) pend.Add(new(2, "Pendente", "aviso", d.Nome, "Documento obrigatório ainda não enviado", "empresa", d.TipoId));
            else if (d.Validade is { } v && v >= hoje && v <= hoje.AddDays(30) && d.Situacao != StatusDocumento.EmValidacao)
                pend.Add(new(v <= hoje.AddDays(7) ? 2 : 3, $"Vence em {v.DayNumber - hoje.DayNumber} dias", "aviso", d.Nome, $"Validade {v:dd/MM/yyyy}", "empresa", d.TipoId));
        }
        var abertos = SincronizacaoCadastros.CotacoesAbertas;
        var cots = await (from cf in db.Set<CotacaoFornecedor>().AsNoTracking()
                          join c in db.Cotacoes.AsNoTracking() on cf.CotacaoId equals c.Id
                          where cf.FornecedorId == s.Fornecedor.Id && abertos.Contains(c.Status) && cf.Status != StatusConvite.Declinou && (c.PrazoResposta == null || c.PrazoResposta > agora)
                          select new { c.Id, c.Numero, c.Nome, c.PrazoResposta, cf.Status, cf.RascunhoEm }).ToListAsync(ct);
        foreach (var c in cots.Where(c => c.Status == StatusConvite.Convidado))
        {
            var horas = c.PrazoResposta is { } pr ? (pr - agora).TotalHours : (double?)null;
            pend.Add(new(horas is < 48 ? 1 : 3, horas is { } h ? (h < 24 ? $"Fecha em {Math.Max(1, (int)h)} h" : $"Fecha {c.PrazoResposta!.Value.ToLocalTime():dd/MM HH'h'}") : "Aberta",
                horas is < 48 ? "aviso" : "info", $"Cotação {c.Numero} · {c.Nome}", c.RascunhoEm is null ? "Proposta não enviada" : "Rascunho salvo, ainda não enviado", "cot", c.Id));
        }
        var cols = s.Colaboradores.Where(c => c.Situacao != SituacaoColaborador.DesligadoDaObra.ToString()).ToList();
        foreach (var c in cols.Where(c => c.Situacao is nameof(SituacaoColaborador.Bloqueado) or nameof(SituacaoColaborador.Pendente) or nameof(SituacaoColaborador.AptoComRestricao)))
            pend.Add(new(c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? 1 : 3, c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? "Funcionário bloqueado" : "Funcionário",
                c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? "erro" : "aviso", c.Nome, c.Pendencias ?? "Documentos pendentes", "func", c.Id));
        var comps = await CompetenciasAsync(token, ct);
        foreach (var c in comps.Where(c => c.Situacao is SituacaoCompetencia.Aberta or SituacaoCompetencia.ComPendencia))
            pend.Add(new(c.Atrasada ? 1 : 2, c.Atrasada ? "Atrasada" : c.Situacao == SituacaoCompetencia.ComPendencia ? "Com pendência" : "Competência aberta", c.Atrasada ? "erro" : "info",
                $"Documentação mensal {c.Mes:MM/yyyy} · contrato {c.Contrato} {c.Obra}", $"{c.Entregues} de {c.Total} itens · limite {c.Limite:dd/MM}", "med", c.Id));
        var bloqueado = TravasQualidade.DocumentosBloqueantes(s.Fornecedor, hoje).ToList();
        var st = s.Fornecedor.CalcularStatus(hoje);
        var (sit, tom, motivo) = bloqueado.Count > 0 || st is StatusQualificacao.Desqualificado
            ? ("Bloqueado", "erro", bloqueado.Count > 0 ? string.Join(" ", bloqueado) + " Pedidos e medições ficam bloqueados até regularizar." : "Fornecedor desqualificado. Fale com Suprimentos.")
            : pend.Count > 0 ? ("Com pendência", "aviso", pend.OrderBy(p => p.Urgencia).First() is var p1 ? $"{p1.Titulo}: {p1.Detalhe}." : "")
            : ("Apto", "ok", "Tudo em dia.");
        var comp = comps.Where(c => c.Situacao != SituacaoCompetencia.Conferida).OrderByDescending(c => c.Mes).FirstOrDefault();
        var emails = s.Fornecedor.Contatos.Where(c => c.RecebeAvisos && !string.IsNullOrWhiteSpace(c.Email)).Select(c => c.Email!).ToList();
        if (emails.Count == 0 && !string.IsNullOrWhiteSpace(s.Fornecedor.Emails)) emails = s.Fornecedor.Emails.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList();
        var docsAtivos = s.Documentos.Where(d => !d.PorEntrega).ToList();
        var urgente = cots.Where(c => c.PrazoResposta != null).OrderBy(c => c.PrazoResposta).FirstOrDefault();
        return new InicioPortal(sit, tom, motivo, cots.Count, urgente is null ? null : $"cotação {urgente.Numero} fecha {urgente.PrazoResposta!.Value.ToLocalTime():dd/MM HH'h'}",
            docsAtivos.Count(d => d.Situacao is StatusDocumento.Valido or StatusDocumento.AVencer), docsAtivos.Count, docsAtivos.Count(d => d.Situacao == StatusDocumento.AVencer),
            docsAtivos.Count(d => d.Situacao == StatusDocumento.Reprovado), cols.Count, cols.Count(c => c.Situacao is nameof(SituacaoColaborador.Bloqueado) or nameof(SituacaoColaborador.Pendente)),
            cols.Count(c => c.AguardandoValidacao), comp is null ? null : $"{comp.Mes:MM/yyyy}", comp?.Entregues ?? 0, comp?.Total ?? 0,
            pend.OrderBy(p => p.Urgencia).ThenBy(p => p.Titulo).ToList(), emails);
    }

    public async Task<IReadOnlyList<PedidoPortal>> PedidosAsync(string token, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var visiveis = new[] { StatusPedido.Aprovado, StatusPedido.EnviadoFornecedor, StatusPedido.EntregueParcial, StatusPedido.Entregue };
        var peds = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Where(p => p.FornecedorId == s.Fornecedor.Id && visiveis.Contains(p.Status))
            .OrderByDescending(p => p.Numero).Take(100).ToListAsync(ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return peds.Select(p => new PedidoPortal(p.Id, p.Numero, obras.GetValueOrDefault(p.ObraId, ""), DateOnly.FromDateTime(p.CriadoEm.ToLocalTime().Date), p.Entrega, p.Status, p.Valor,
            p.Itens.Select(i => (i.Descricao, i.Unidade ?? "", i.Quantidade, i.QuantidadeRecebidaAceita)).ToList())).ToList();
    }

    public async Task<IReadOnlyList<VersaoDocumentoPortal>> VersoesDocumentoAsync(string token, Guid tipoId, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var l = await db.FornecedorDocumentos.AsNoTracking().Include(d => d.TipoDocumento).Where(d => d.FornecedorId == s.Fornecedor.Id && d.TipoDocumentoExigidoId == tipoId)
            .OrderByDescending(d => d.CriadoEm).Take(30).ToListAsync(ct);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var vigente = l.Where(d => d.Status is not (StatusDocumento.EmValidacao or StatusDocumento.Reprovado) && d.ArquivoBlob is not null).Select(d => (Guid?)d.Id).FirstOrDefault();
        return l.Select(d => new VersaoDocumentoPortal(d.Id, d.CriadoEm, d.ResponsavelEnvio ?? d.CriadoPor, d.DataValidade, StatusDocumentoCalculo.Calcular(d, hoje), d.MotivoRecusa, d.ArquivoNomeOriginal, d.Id == vigente)).ToList();
    }
}
