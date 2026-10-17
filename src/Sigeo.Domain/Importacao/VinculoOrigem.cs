using Sigeo.Domain.Common;

namespace Sigeo.Domain.Importacao;

/// <summary>
/// De-para de importação, reaproveitado nas próximas cargas: texto do sistema de origem (insumo, fornecedor, fase, serviço/local do ERP)
/// → cadastro do Sigeo. Ignorar = a origem não entra (ex.: insumo de teste). Local vale por obra; os demais, pela construtora.
/// </summary>
public class VinculoOrigem : EntidadeAuditavel
{
    public TipoVinculoOrigem Tipo { get; set; }
    /// <summary>Sistema de origem (ex.: "ERP").</summary>
    public string Sistema { get; set; } = "ERP";
    public Guid? ObraId { get; set; }
    /// <summary>Texto da origem normalizado (sem acento/caixa/espaços extras) — é a chave.</summary>
    public string Chave { get; set; } = string.Empty;
    public string TextoOrigem { get; set; } = string.Empty;
    /// <summary>Produto, fornecedor ou local escolhido.</summary>
    public Guid? DestinoId { get; set; }
    /// <summary>Fase (grupo de serviço) escolhida.</summary>
    public string? DestinoTexto { get; set; }
    public bool Ignorar { get; set; }
}
