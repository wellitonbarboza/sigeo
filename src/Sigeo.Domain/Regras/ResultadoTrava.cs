using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Domain.Regras;

/// <summary>Resultado de uma verificação de trava: lista de bloqueios legíveis (vazia = liberado).</summary>
public sealed record ResultadoTrava(IReadOnlyList<string> Bloqueios)
{
    public static ResultadoTrava Ok { get; } = new([]);
    public bool Liberado => Bloqueios.Count == 0;
    public string? Resumo => Liberado ? null : string.Join(" | ", Bloqueios);
}
