using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D11 · Regra de retenção de registros (armazenamento, recuperação, proteção, retenção, descarte).</summary>
public class RegraRetencao : EntidadeAuditavel
{
    public string Identificacao { get; set; } = string.Empty;
    /// <summary>Código do formulário/tabela que gera o registro (ex.: F.SGQ.03, T07, FORM.AC-03).</summary>
    public string? Formulario { get; set; }
    public string Armazenamento { get; set; } = string.Empty;
    public string Recuperacao { get; set; } = string.Empty;
    public string Protecao { get; set; } = string.Empty;
    public string TempoRetencao { get; set; } = string.Empty;
    /// <summary>Anos numéricos quando aplicável (nulo = até o fim da obra/permanente).</summary>
    public int? TempoRetencaoAnos { get; set; }
    public string Descarte { get; set; } = string.Empty;
}
