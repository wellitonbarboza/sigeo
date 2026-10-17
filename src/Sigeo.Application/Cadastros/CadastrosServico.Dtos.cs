using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

public enum OrdemProdutos { Codigo, Nome }

/// <summary>Filtro da lista de produtos. Texto procura em código e nome; Grupo é exato; Ativo nulo = todos.</summary>
public sealed record FiltroProdutos(string? Texto = null, string? Grupo = null, bool? Ativo = null, OrdemProdutos Ordem = OrdemProdutos.Codigo, bool Decrescente = false,
    FiltroCadastroProduto Cadastro = FiltroCadastroProduto.Todos);

/// <summary>Filtro "Cadastro" da tela de produtos: padronizados × antigos (fora do catálogo + a classificar).</summary>
public enum FiltroCadastroProduto { Todos = 0, Padronizados = 1, Antigos = 2, AClassificar = 3, ForaDoCatalogo = 4 }

/// <summary>Linha da grade de produtos: só as colunas exibidas (o catálogo tem milhares de itens).</summary>
public sealed record LinhaProduto(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, string? Ncm,
    int LeadTimeDias, decimal? PrecoReferencia, string? MaterialControlado, bool Ativo,
    SituacaoCadastroProduto Situacao = SituacaoCadastroProduto.Padronizado, string? SubstituidoPor = null);

/// <summary>Página da consulta; <see cref="Pagina"/> volta ajustada quando a pedida passou do fim (ex.: filtro reduziu o total).</summary>
public sealed record PaginaProdutos(IReadOnlyList<LinhaProduto> Itens, int Total, int Pagina, int Tamanho);

/// <summary>Linha completa para a planilha exportada (inclui as colunas que não cabem na grade).</summary>
public sealed record LinhaProdutoExportacao(string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, string? Ncm, string? DescricaoCompleta,
    string? Marca, string? Referencia, string? Ean, decimal? QuantidadePorEmbalagem, string? CodigoAnterior, string? OrigemSistema,
    int LeadTimeDias, decimal? PrecoReferencia, string? MaterialControlado, bool Ativo);

public sealed record ResumoProdutos(int Ativos, int AClassificar, int SemNcm, int Vinculados, int Padronizados = 0, int ForaDoCatalogo = 0);
