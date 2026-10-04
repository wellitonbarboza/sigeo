using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;

namespace QualyHub.Application.Servicos;

/// <summary>Requisição de compra (T01–T03): rascunho, quebra por local/SPE e envio para autorização.</summary>
public sealed class RequisicoesServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T01–T03 Requisição ─────────

    public async Task<Guid> SalvarRequisicaoAsync(Requisicao r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (r.Numero == 0)
        {
            r.Numero = await Numerador.ProximoAsync(db, "Requisicao", ct);
            r.SolicitanteUsuarioId = usuario.UsuarioId ?? Guid.Empty;
            r.SolicitanteNome = usuario.Nome;
        }
        // Situação vem do banco (a tela pode estar desatualizada).
        var statusAtual = await db.Requisicoes.Where(x => x.Id == r.Id).Select(x => (StatusRequisicao?)x.Status).FirstOrDefaultAsync(ct) ?? r.Status;
        if (statusAtual is not (StatusRequisicao.Rascunho or StatusRequisicao.DevolvidaAjuste))
            throw new RegraDeNegocioException("Requisição já enviada: não pode ser alterada.");
        r.RecalcularPrazo();
        if (r.EstabelecimentoFaturamentoId is null && r.Tipo == TipoRequisicao.Material)
            r.EstabelecimentoFaturamentoId = await EstabelecimentosServico.PadraoFaturamentoAsync(db, ct);
        // SPE beneficiária sai dos locais escolhidos (todos do mesmo condomínio).
        var locIds = r.Itens.SelectMany(i => i.Locais).Select(l => l.LocalId).Distinct().ToList();
        if (locIds.Count > 0)
        {
            var doLocal = await db.Locais.Where(l => locIds.Contains(l.Id) && l.ObraId == r.ObraId).Select(l => new { l.Id, l.SpeId }).ToListAsync(ct);
            if (doLocal.Count != locIds.Count) throw new RegraDeNegocioException("Há locais que não pertencem à obra da requisição.");
            var spes = doLocal.Select(l => l.SpeId).Distinct().ToList();
            if (spes.Count(x => x != null) > 1) throw new RegraDeNegocioException("Os itens têm locais de SPEs (condomínios) diferentes: faça uma requisição por SPE.");
            if (spes.FirstOrDefault(x => x != null) is { } sp) r.SpeId = sp;
        }
        // Netos (quebra por local) não são cobertos pelo Grafo: grava à parte.
        // Linhas repetidas (mesmo local/serviço) são somadas.
        var locais = r.Itens.ToDictionary(i => i.Id, i => i.Locais.GroupBy(l => (l.LocalId, l.ComposicaoId))
            .Select(g => new RequisicaoItemLocal { RequisicaoItemId = i.Id, LocalId = g.Key.LocalId, ComposicaoId = g.Key.ComposicaoId, Quantidade = g.Sum(l => l.Quantidade) }).ToList());
        foreach (var i in r.Itens) i.Locais = [];
        // Remove a quebra de TODOS os itens da requisição (inclusive itens excluídos na tela, senão a FK impede excluir o item).
        db.RequisicaoItemLocais.RemoveRange(await db.RequisicaoItemLocais.Where(x => db.RequisicaoItens.Any(i => i.Id == x.RequisicaoItemId && i.RequisicaoId == r.Id)).ToListAsync(ct));
        await Grafo.SalvarAsync(db, r, nameof(Requisicao.Itens));
        db.RequisicaoItemLocais.AddRange(locais.Values.SelectMany(x => x));
        await db.SaveChangesAsync(ct);
        foreach (var i in r.Itens) i.Locais = locais[i.Id];
        return r.Id;
    }

    public async Task EnviarRequisicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.Include(x => x.Itens).ThenInclude(i => i.Locais).FirstAsync(x => x.Id == id, ct);
        var temEscopo = await db.Anexos.AnyAsync(a => a.Entidade == nameof(Requisicao) && a.EntidadeId == id && a.Categoria == "Escopo", ct);
        // Serviço vindo do orçamento: a planilha serviço × local é o escopo (projetos/ITS seguem como anexo opcional).
        if (r.Tipo == TipoRequisicao.Servico && r.Itens.Any(i => i.Locais.Count > 0)) temEscopo = true;
        var erros = r.ValidarEnvio(temEscopo).ToList();
        if (r.SpeId is null && await db.Spes.AnyAsync(s => s.ObraId == r.ObraId && s.Ativa, ct))
            erros.Add("Informe a SPE beneficiária (destino do custo) — ou puxe os itens do orçamento pelos locais.");
        if (r.EstabelecimentoFaturamentoId is null && await db.Estabelecimentos.AnyAsync(e => e.Ativo, ct))
            erros.Add("Informe o \"Faturar para\" (CNPJ que compra).");
        foreach (var i in r.Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado && i.Locais.Count > 0 && i.Quantidade > i.Locais.Sum(l => l.Quantidade) + 0.001m))
            if (string.IsNullOrWhiteSpace(r.JustificativaAcimaOrcado))
                erros.Add($"{i.Descricao}: quantidade acima do saldo orçado para os locais ({i.Locais.Sum(l => l.Quantidade):0.###}) — justifique.");
        if (r.SpeId is { } sid && !await db.Spes.AnyAsync(s => s.Id == sid && s.ObraId == r.ObraId && s.Ativa, ct))
            erros.Add("A SPE informada não pertence a esta obra ou está inativa.");
        ValidacaoException.Se(erros);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Requisicao, r.Id, r.ObraId, $"Requisição {r.Numero} – {r.Atividade}", r.ValorEstimado, ct);
        r.Status = StatusRequisicao.AguardandoEngenheiro;
        r.MotivoReprovacao = null;
        await db.SaveChangesAsync(ct);
    }
}
