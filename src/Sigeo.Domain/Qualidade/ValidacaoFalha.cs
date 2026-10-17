using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Erro de validação de domínio (lista de mensagens legíveis).</summary>
public sealed class ValidacaoFalha(IReadOnlyList<string> erros) : RegraDeNegocioException(string.Join(" | ", erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;
    public static void Se(IReadOnlyList<string> erros) { if (erros.Count > 0) throw new ValidacaoFalha(erros); }
}
