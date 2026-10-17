using Sigeo.Domain.Common;

namespace Sigeo.Domain.Financeiro;

/// <summary>Conta do plano de contas gerencial (hierárquico pelo código: 2 › 2.1 › 2.1.3). Lança-se só na conta analítica (sem filhas).</summary>
public class ContaGerencial : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public NaturezaConta Natureza { get; set; } = NaturezaConta.Despesa;
    public bool Ativa { get; set; } = true;

    /// <summary>Código do pai ("2.1.3" → "2.1"; "2" → null).</summary>
    public static string? CodigoPai(string codigo) => codigo.LastIndexOf('.') is var i and > 0 ? codigo[..i] : null;
    public int Nivel => Codigo.Count(c => c == '.') + 1;
    public string Rotulo => $"{Codigo} {Nome}";
}
