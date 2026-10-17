using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Engenharia;

public sealed record KitTipologia(
    IReadOnlyList<KitTip> Tipologias, IReadOnlyList<KitAmbiente> Ambientes, IReadOnlyList<KitMix> Mix,
    IReadOnlyList<KitServico> Servicos, IReadOnlyList<KitAplicacao> Aplicacoes, IReadOnlyList<KitInsumo>? Insumos = null)
{
    public IReadOnlyList<KitInsumo> InsumosOuVazio => Insumos ?? [];
}

/// <summary>
/// Insumo da composição (aba Insumos, opcional): quantidade por unidade do serviço e custo unitário.
/// Material aponta para o produto pelo código; sem código, procura pelo nome e unidade ou cria o produto (grupo/subgrupo da planilha).
/// </summary>
public sealed record KitInsumo(int Linha, string Servico, TipoInsumo Tipo, string Descricao, string Unidade, decimal Coeficiente, decimal Perda,
    decimal CustoUnitario, string? Produto, string? GrupoProduto, string? SubgrupoProduto);

public sealed record KitTip(int Linha, string Codigo, string Nome, TipoTipologia Tipo, int Dormitorios, int Suites, decimal AreaPrivativa,
    decimal? AreaEquivalente, int Pavimentos, bool PrimeiroTerreo, bool NumeracaoPorAndar, decimal? PeDireitoPadrao, string? Observacoes);

public sealed record KitAmbiente(int Linha, string Tipologia, int Ordem, EscopoAmbiente Escopo, string Nome, string Sigla, string? Categoria,
    decimal AreaPiso, decimal Perimetro, decimal? PeDireito, decimal AreaVaos, int? PavimentoDe, int? PavimentoAte);

public sealed record KitMix(int Linha, string Torre, int PavimentoDe, int PavimentoAte, string Apartamento, int Quantidade);

public sealed record KitServico(int Linha, string Codigo, string Descricao, string Unidade, string? Grupo, RegraQuantidade Regra, decimal Fator, string? Its, string? Observacao);

public sealed record KitAplicacao(int Linha, string Tipologia, string Servico, AlvoServico Alvo, string? Ambiente, decimal? QuantidadeFixa);

public sealed record ResumoKit(int TipologiasNovas, int TipologiasAtualizadas, int Ambientes, int ServicosNovos, int ServicosAtualizados, int Aplicacoes,
    IReadOnlyList<string> ItsNaoEncontradas, IReadOnlyList<string> Avisos, int Insumos = 0, int ServicosComInsumos = 0, int ProdutosNovos = 0);
