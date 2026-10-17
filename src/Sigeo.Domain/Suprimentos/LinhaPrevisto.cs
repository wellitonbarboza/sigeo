using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public sealed record LinhaPrevisto(string Descricao, decimal Quantidade, string Base, decimal Unitario, decimal Total);
