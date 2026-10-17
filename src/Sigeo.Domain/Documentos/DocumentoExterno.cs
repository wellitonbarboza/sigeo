using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D10 · Documento externo (NBR, legislação, projeto do cliente) com verificação periódica de atualização.</summary>
public class DocumentoExterno : EntidadeAuditavel, IHistoricoTemporal
{
    public string Identificacao { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Edicao { get; set; } = string.Empty;
    /// <summary>Lista EmissorExterno (ABNT, INMETRO, PBQP-H, Prefeitura, Bombeiros, Cliente…).</summary>
    public string Emissor { get; set; } = string.Empty;
    public DateOnly DataVerificacao { get; set; }
    public SituacaoDocExterno Situacao { get; set; } = SituacaoDocExterno.Vigente;
    /// <summary>Códigos dos documentos internos que citam este (separados por ;).</summary>
    public string? Relacionados { get; set; }
    public string? ArquivoOuLink { get; set; }
    /// <summary>Edição anterior que esta substitui (ex.: NBR 15217:2018).</summary>
    public string? Substitui { get; set; }

    public DateOnly ProximaVerificacao(int meses) => DataVerificacao.AddMonths(meses);
}
