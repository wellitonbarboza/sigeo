using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Grupo padrão: sigla (só ao criar), tipo de custo e fase do orçamento herdados, material controlado ligado (1 grupo = 1 material).</summary>
public sealed record PadraoGrupo(int Linha, string Grupo, string? Sigla, TipoCusto? TipoCusto, string? Fase, string? MaterialCodigo);

/// <summary>Regra de reclassificação: produtos do grupo/subgrupo atual (opcionalmente só os que contêm uma das palavras) vão para o grupo/subgrupo novo.</summary>
public sealed record RegraReclassificacao(int Linha, string GrupoAtual, string? SubAtual, string? Contem, string GrupoNovo, string SubNovo, string? Motivo);

/// <summary>Insumo a cadastrar (ou completar) já classificado.</summary>
public sealed record ProdutoPadrao(int Linha, string Nome, string Unidade, string Grupo, string SubGrupo, TipoCusto? TipoCusto, string? CodigoErp, decimal? Preco);

public sealed record PadraoCatalogo(IReadOnlyList<PadraoGrupo> Grupos, IReadOnlyList<RegraReclassificacao> Regras, IReadOnlyList<ProdutoPadrao> Produtos)
{
    /// <summary>Os materiais da aba Grupos são a lista oficial: os demais deixam de ligar grupos e produtos.</summary>
    public bool ListaOficialMateriais { get; init; } = true;
}

public sealed record ResumoPadraoCatalogo(
    int ProdutosMovidos, IReadOnlyList<(string Regra, int Produtos)> PorRegra, IReadOnlyList<string> GruposCriados, int GruposAtualizados,
    IReadOnlyList<string> MateriaisLigados, int ProdutosNovos, int ProdutosCompletados, IReadOnlyList<string> CategoriasDesativadas, IReadOnlyList<string> Avisos);
