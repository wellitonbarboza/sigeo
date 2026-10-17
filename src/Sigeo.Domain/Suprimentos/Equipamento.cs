using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Equipamento rastreado um a um (patrimônio próprio, locado ou de terceiro em uso na obra).</summary>
public class Equipamento : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Nº de patrimônio / etiqueta.</summary>
    public string Patrimonio { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public Guid FamiliaId { get; set; }
    /// <summary>Lote do mesmo item locado (ex.: 80 módulos de andaime); próprio = 1.</summary>
    public decimal Quantidade { get; set; } = 1;
    public PropriedadeEquipamento Propriedade { get; set; } = PropriedadeEquipamento.Propria;
    public Guid? FornecedorId { get; set; }
    /// <summary>Locado: contrato de locação (cobrança recorrente).</summary>
    public Guid? ContratoId { get; set; }
    public Guid? ContratoItemId { get; set; }
    public Criticidade Criticidade { get; set; } = Criticidade.B;
    public string? Fabricante { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public int? Ano { get; set; }
    public decimal? ValorAquisicao { get; set; }
    public DateOnly? DataAquisicao { get; set; }
    /// <summary>Aluguel interno por dia apropriado na obra onde o equipamento está (próprio).</summary>
    public decimal? DiariaInterna { get; set; }
    /// <summary>Onde está (almoxarifado central ou da obra).</summary>
    public Guid? AlmoxarifadoId { get; set; }
    public SituacaoEquipamento Situacao { get; set; } = SituacaoEquipamento.Disponivel;
    public decimal Horimetro { get; set; }
    public decimal Hodometro { get; set; }
    /// <summary>Valores dos campos extras da família ("Placa=ABC1D23;RENAVAM=…").</summary>
    public string? CamposExtras { get; set; }
    /// <summary>Locado: data combinada para devolver.</summary>
    public DateOnly? DevolverAte { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
    public string? Observacao { get; set; }
    public ICollection<EquipamentoDocumento> Documentos { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Patrimonio)) e.Add("Informe o nº de patrimônio / identificação.");
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome do equipamento.");
        if (FamiliaId == Guid.Empty) e.Add("Informe a família.");
        if (Propriedade == PropriedadeEquipamento.Locada && ContratoId is null && FornecedorId is null) e.Add("Equipamento locado: informe o contrato ou a locadora.");
        return e;
    }

    public string Campo(string nome) =>
        (CamposExtras ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).FirstOrDefault(p => p[0].Trim() == nome) is { Length: 2 } kv ? kv[1] : "";
}
