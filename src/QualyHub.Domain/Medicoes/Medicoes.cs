using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Medicoes;

/// <summary>Medição de contrato/empreiteiro importada do Obra Prima. Só aprova com FVS dos serviços controlados aprovadas.</summary>
public class Medicao : EntidadeDaObra, IOrigemObraPrima, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public string? NumeroContrato { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFim { get; set; }
    public decimal ValorMedido { get; set; }
    public decimal ValorRetencao { get; set; }
    public StatusAprovacao Status { get; set; } = StatusAprovacao.Rascunho;
    public string? Bloqueios { get; set; }
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<MedicaoItem> Itens { get; set; } = [];
}

public class MedicaoItem : Entidade
{
    public Guid MedicaoId { get; set; }
    public Medicao Medicao { get; set; } = null!;
    public string CodigoItem { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal QuantidadeMedida { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    /// <summary>Local medido (quando o export informa). Usado para casar com a FVS do mesmo local.</summary>
    public Guid? LocalId { get; set; }
    public string? LocalTexto { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
}
