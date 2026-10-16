using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Servicos;

/// <summary>Requisição de compra (T01–T03): rascunho, quebra por local/SPE e envio para autorização.</summary>
public sealed class RequisicoesServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T01–T03 Requisição ─────────

    /// <summary>
    /// Requisição criada direto do orçamento (Engenharia › Orçamento › Contratar/comprar): mesmo caminho da tela de requisição —
    /// itens com a quebra por local (saldo orçado), produto herdado do cadastro (controlado, especificação, prazo), SPE dos locais,
    /// faturamento padrão, endereço da obra, número e solicitante. Fica em rascunho para revisar e enviar.
    /// </summary>
    public async Task<Guid> CriarDoOrcamentoAsync(Guid obraId, TipoRequisicao tipo, IReadOnlyList<LinhaDemanda> demanda, string? grupo,
        IReadOnlyList<string> atividades, DateOnly hoje, CancellationToken ct = default)
    {
        var r = new Requisicao
        {
            ObraId = obraId, Tipo = tipo, DataEntrega = hoje.AddDays(15),
            CategoriaServico = tipo == TipoRequisicao.Servico ? "Serviço de obra" : null,
            Atividade = AtividadeDoGrupo(grupo, atividades),
            Observacao = tipo == TipoRequisicao.Servico ? "Gerada pelo orçamento por local (serviço × local × quantidade)." : "Gerada pelo orçamento por local (material das composições × local)."
        };
        await using (var db = fabrica.CriarLeitura())
        {
            r.EnderecoEntrega = await db.Obras.Where(o => o.Id == obraId).Select(o => o.EnderecoEntrega).FirstOrDefaultAsync(ct);
            var ids = demanda.Where(d => d.ProdutoId != null).Select(d => d.ProdutoId!.Value).Distinct().ToList();
            var prods = await db.Produtos.AsNoTracking().Include(p => p.MaterialControlado).Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
            foreach (var i in OrcamentoServico.Itens(r.Id, demanda, prods)) r.Itens.Add(i);
        }
        if (r.Itens.Count == 0) throw new RegraDeNegocioException("Não há saldo a requisitar para os serviços e locais escolhidos.");
        return await SalvarRequisicaoAsync(r, ct);
    }

    /// <summary>Atividade da lista que combina com o grupo da composição (primeira palavra); senão a primeira da lista.</summary>
    public static string AtividadeDoGrupo(string? grupo, IReadOnlyList<string> atividades)
    {
        static string Sem(string s) => new string(s.Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(grupo))
        {
            var chave = Sem(grupo).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            chave = chave[..Math.Min(6, chave.Length)];
            if (atividades.FirstOrDefault(a => Sem(a).Contains(chave)) is { } a) return a;
        }
        return atividades.FirstOrDefault() ?? "Serviços";
    }

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
        var marcas = await MarcasServico.NormalizarAsync(db, r.Itens.Select(i => i.Marca), ct);
        foreach (var i in r.Itens) { i.Marca = MarcasServico.Oficial(marcas, i.Marca); if (i.Marca is null) i.MesmaMarca = false; }
        // Obra própria (sem SPE): o CNPJ da construtora dono da obra é o "Faturar para" padrão.
        if (r.EstabelecimentoFaturamentoId is null)
            r.EstabelecimentoFaturamentoId = await db.Obras.Where(o => o.Id == r.ObraId && o.DonoCusto == DonoCustoObra.Construtora).Select(o => o.EstabelecimentoDonoId).FirstOrDefaultAsync(ct);
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
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Requisicao, r.Id, r.ObraId, $"Requisição {r.Numero} – {r.Atividade}", r.ValorEstimado, ct, r.SpeId);
        r.Status = StatusRequisicao.AguardandoEngenheiro;
        r.MotivoReprovacao = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// O solicitante tira a requisição da aprovação e volta a editá-la — só enquanto a 1ª aprovação não foi dada.
    /// </summary>
    public async Task VoltarParaEdicaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.FirstAsync(x => x.Id == id, ct);
        if (r.Status != StatusRequisicao.AguardandoEngenheiro)
            throw new RegraDeNegocioException("Só dá para voltar para edição enquanto a requisição aguarda a primeira aprovação.");
        var s = await db.SolicitacoesAprovacao.Include(x => x.Etapas)
            .FirstOrDefaultAsync(x => x.Entidade == EntidadeAprovavel.Requisicao && x.EntidadeId == id && x.Status == StatusAprovacao.EmAprovacao, ct)
            ?? throw new RegraDeNegocioException("Aprovação em andamento não encontrada.");
        s.RetirarPeloSolicitante(usuario.UsuarioId ?? Guid.Empty, relogio.GetUtcNow());
        r.Status = StatusRequisicao.Rascunho;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Exclui (logicamente) a requisição em edição — pelo solicitante ou administrador.</summary>
    public async Task ExcluirRequisicaoAsync(Guid id, bool administrador, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Requisicoes.FirstAsync(x => x.Id == id, ct);
        if (r.Status is not (StatusRequisicao.Rascunho or StatusRequisicao.DevolvidaAjuste))
            throw new RegraDeNegocioException("Só é possível excluir requisição em edição. Se estiver em aprovação, volte para edição antes.");
        if (!administrador && r.SolicitanteUsuarioId != usuario.UsuarioId)
            throw new RegraDeNegocioException("Só quem solicitou pode excluir a requisição.");
        if (await db.Cotacoes.AnyAsync(c => c.RequisicaoId == id, ct))
            throw new RegraDeNegocioException("A requisição já tem cotação: não pode ser excluída.");
        db.Requisicoes.Remove(r); // exclusão lógica (interceptor de auditoria)
        await db.SaveChangesAsync(ct);
    }
}
