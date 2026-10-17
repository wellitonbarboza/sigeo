using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Tipo de equipamento de medição (trena, nível, prumo…): padrões configuráveis por construtora.</summary>
public class TipoEquipamentoMedicao : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    /// <summary>Prefixo do código (TR, NV, PR…).</summary>
    public string Prefixo { get; set; } = string.Empty;
    public int PeriodicidadeMeses { get; set; } = 6;
    public string Criterio { get; set; } = string.Empty;
    public string? Metodo { get; set; }
    public TipoControleEquipamento TipoControle { get; set; } = TipoControleEquipamento.VerificacaoInterna;
    public bool Ativo { get; set; } = true;
}
