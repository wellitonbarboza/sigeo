using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S01/S04 — Colaboradores e documentos ═══════════════════════

/// <summary>S01 · Pessoa que trabalha na obra (própria ou terceira). CPF só como hash + máscara (LGPD).</summary>
public class Colaborador : EntidadeDaObra, IHistoricoTemporal
{
    public string Nome { get; set; } = string.Empty;
    public string CpfHash { get; set; } = string.Empty;
    public string CpfMascarado { get; set; } = string.Empty;
    public VinculoColaborador Vinculo { get; set; } = VinculoColaborador.Terceiro;
    public Guid? FornecedorId { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid FuncaoObraId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataSaida { get; set; }
    public string? Telefone { get; set; }
    public bool ConsentimentoTelefone { get; set; }
    public SituacaoColaborador Situacao { get; set; } = SituacaoColaborador.Pendente;
    public AtividadeRisco Restricoes { get; set; }
    public string? Pendencias { get; set; }
    /// <summary>Cadastrado pelo fornecedor no portal: só entra na obra depois da validação da construtora.</summary>
    public bool CadastradoPeloFornecedor { get; set; }
    public bool AguardandoValidacao { get; set; }
    public string? ValidadoPor { get; set; }
    public DateTimeOffset? ValidadoEm { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();

    public static string Mascarar(string cpfDigitos) =>
        cpfDigitos.Length == 11 ? $"***.{cpfDigitos.Substring(3, 3)}.***-{cpfDigitos[^2..]}" : "***";
}
