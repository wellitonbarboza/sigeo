using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Quadra / condomínio = SPE. O tipo é configurável (lista "Tipo de agrupamento"). Recebe as torres e as áreas comuns do condomínio.
/// </summary>
public class Quadra : EntidadeAuditavel, IHistoricoTemporal, IPertenceObra
{
    public Guid ObraId { get; set; }
    /// <summary>Sigla usada nos códigos de local (ex.: QC).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Condomínio";
    /// <summary>Nome do condomínio (quando diferente do nome da quadra).</summary>
    public string? NomeCondominio { get; set; }
    public string? CnpjCondominio { get; set; }
    public string? Endereco { get; set; }
    public string? Matricula { get; set; }
    public decimal? AreaTerreno { get; set; }
    public string? Observacoes { get; set; }
    public int Ordem { get; set; }
    public bool Ativa { get; set; } = true;
    /// <summary>Implantação (tipologia com as áreas comuns do condomínio).</summary>
    public Guid? ImplantacaoId { get; set; }
    /// <summary>Local raiz criado pelo gerador (Condomínio › Torre › Pavimento › Apartamento › Cômodo).</summary>
    public Guid? LocalId { get; set; }
}
