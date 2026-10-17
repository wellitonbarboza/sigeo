using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Linha de necessidade de material (kit de um local, ou soma de vários).</summary>
public sealed record NecessidadeMaterial(Guid ProdutoId, Guid? ComposicaoId, decimal Quantidade, decimal CustoOrcado);
