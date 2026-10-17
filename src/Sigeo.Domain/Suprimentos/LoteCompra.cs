using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// Parte da requisição que cabe a um comprador (itens da mesma carteira). Nasce na autorização da requisição pelo
/// encaminhamento automático; cada lote segue sozinho (cotação, pedidos, recebimentos).
/// </summary>
public class LoteCompra : EntidadeDaObra
{
    public Guid RequisicaoId { get; set; }
    /// <summary>Letra do lote dentro da requisição (A, B, C…).</summary>
    public string Letra { get; set; } = "A";
    /// <summary>Categoria que gerou o lote (grupo, grupo › subgrupo ou categoria de serviço).</summary>
    public string Categoria { get; set; } = string.Empty;
    /// <summary>Comprador atual; nulo = fila do gestor de suprimentos.</summary>
    public Guid? CompradorUsuarioId { get; set; }
    public string? CompradorNome { get; set; }
    /// <summary>Titular da carteira quando o lote está com o substituto (ausência).</summary>
    public Guid? TitularUsuarioId { get; set; }
    public string? TitularNome { get; set; }
    public SituacaoLote Situacao { get; set; } = SituacaoLote.EmCompra;
    /// <summary>Data limite para comprar: necessidade − lead time do item mais demorado.</summary>
    public DateOnly? DataLimiteCompra { get; set; }
    public DateTimeOffset AtribuidoEm { get; set; }
    public string? MotivoReatribuicao { get; set; }

    public bool ComSubstituto => TitularUsuarioId is not null && TitularUsuarioId != CompradorUsuarioId;
    public bool NaFilaDoGestor => CompradorUsuarioId is null;
}
