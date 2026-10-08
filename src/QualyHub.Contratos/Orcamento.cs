using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Contratos;

public sealed record ResultadoOrcamento(int Criadas, int Atualizadas, int Removidas, int Travadas, decimal Total);

/// <summary>Quantidade orçada e já requisitada de um serviço (composição) num local.</summary>
public sealed record LocalDemanda(Guid LocalId, Guid ComposicaoId, decimal Orcado, decimal Requisitado);

/// <summary>Linha da demanda (material por produto ou serviço por composição) para os locais escolhidos.</summary>
public sealed record LinhaDemanda(Guid? ProdutoId, Guid? ComposicaoId, string Descricao, string Unidade, decimal Orcado, decimal Requisitado,
    decimal ValorReferencia, Guid? ServicoControladoId, string? Grupo, IReadOnlyList<LocalDemanda> Locais)
{
    public decimal Saldo => Math.Max(0, Orcado - Requisitado);
}

/// <summary>Serviço (composição) com células no orçamento da obra (lista do seletor de demanda).</summary>
public sealed record ServicoOrcado(Guid Id, string Codigo, string Descricao, string? Unidade, string? Grupo);

/// <summary>Local para árvores de seleção e totais do orçamento.</summary>
public sealed record LocalOrcamento(Guid Id, Guid? PaiId, string Tipo, string Nome, string Codigo, int Ordem, decimal? AreaPrivativa, Guid? TipologiaId)
{
    public static LocalOrcamento De(Local l) => new(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem, l.AreaPrivativa, l.TipologiaId);
}

/// <summary>Célula serviço × local do orçamento.</summary>
public sealed record CelulaOrcamento(Guid Id, Guid ComposicaoId, Guid LocalId, decimal Quantidade, decimal CustoUnitario, decimal Custo, bool Travado, string? Origem);

public sealed record ComposicaoResumo(Guid Id, string Codigo, string Descricao, string Unidade, string? Grupo);

public sealed record OrcamentoObraDto(IReadOnlyList<LocalOrcamento> Locais, IReadOnlyList<CelulaOrcamento> Celulas,
    IReadOnlyList<ComposicaoResumo> Composicoes, IReadOnlyList<Opcao> Tipologias);

public sealed record EdicaoCelula(decimal Quantidade, bool Travar);

/// <summary>Serviços × locais escolhidos na tela; o servidor recalcula a demanda (a tela não manda quantidades).</summary>
public sealed record PedidoDemanda(TipoRequisicao Tipo, IReadOnlyList<Guid> Composicoes, IReadOnlyList<Guid> Locais);
