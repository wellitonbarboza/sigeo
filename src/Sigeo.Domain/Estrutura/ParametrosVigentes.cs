using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Leitura dos parâmetros com fallback no padrão (o domínio não conhece o banco).</summary>
public sealed class ParametrosVigentes(IReadOnlyDictionary<string, decimal> valores)
{
    public static ParametrosVigentes Padrao { get; } = new(Parametros.Padrao.ToDictionary(k => k.Key, v => v.Value.Valor));

    public decimal this[string chave] =>
        valores.TryGetValue(chave, out var v) ? v
        : Parametros.Padrao.TryGetValue(chave, out var p) ? p.Valor
        : throw new KeyNotFoundException($"Parâmetro '{chave}' não cadastrado.");

    public int Int(string chave) => (int)this[chave];
}
