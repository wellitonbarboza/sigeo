using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Parâmetro do fluxo, editável na tela (limite de alçada, pesos da avaliação, prazos…). Ver <see cref="Parametros"/>.</summary>
public class Parametro : EntidadeAuditavel, IHistoricoTemporal
{
    public string Chave { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Unidade { get; set; }
    public string? Grupo { get; set; }
}
