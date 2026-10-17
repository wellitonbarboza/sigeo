using Sigeo.Domain.Common;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Domain.Colaboracao;

/// <summary>
/// Nota (comentário) de qualquer registro — histórico com data/hora e usuário, nunca um campo único sobrescrito.
/// Imutável depois de gravada: correção = nova nota.
/// </summary>
public class Nota : EntidadeAuditavel
{
    /// <summary>Nome da entidade dona (ex.: "Requisicao", "Cotacao", "Rnc").</summary>
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public string Texto { get; set; } = string.Empty;

    public static Nota Criar(string entidade, Guid entidadeId, string texto)
    {
        var t = (texto ?? "").Trim();
        if (t.Length == 0) throw new RegraDeNegocioException("Escreva a nota.");
        if (t.Length > 4000) throw new RegraDeNegocioException("Nota com mais de 4.000 caracteres.");
        if (string.IsNullOrWhiteSpace(entidade) || entidadeId == Guid.Empty) throw new RegraDeNegocioException("Salve o registro antes de incluir notas.");
        return new Nota { Entidade = entidade, EntidadeId = entidadeId, Texto = t };
    }
}
