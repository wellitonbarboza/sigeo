using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q09 · Calibração (externa, RBC) ou verificação interna.</summary>
public class Calibracao : Entidade
{
    public Guid EquipamentoMedicaoId { get; set; }
    public TipoControleEquipamento Tipo { get; set; }
    public DateOnly Data { get; set; }
    public string Executante { get; set; } = string.Empty;
    public Guid? LaboratorioId { get; set; }
    public string? Certificado { get; set; }
    public string Erro { get; set; } = string.Empty;
    public ResultadoCalibracao Resultado { get; set; } = ResultadoCalibracao.Aprovado;
    public string? AvaliacaoImpacto { get; set; }
    public string? RegistradoPor { get; set; }
}
