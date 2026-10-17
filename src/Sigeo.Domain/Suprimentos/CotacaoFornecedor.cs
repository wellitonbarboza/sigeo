using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T05 · Fornecedor convidado na cotação (guarda a situação de qualificação na data e a proposta).</summary>
public class CotacaoFornecedor : Entidade
{
    public Guid CotacaoId { get; set; }
    public Cotacao Cotacao { get; set; } = null!;
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    /// <summary>Token do link de resposta do fornecedor.</summary>
    public Guid LinkResposta { get; set; } = Guid.NewGuid();
    public StatusConvite Status { get; set; } = StatusConvite.Convidado;
    public DateOnly? DataResposta { get; set; }
    public decimal? ValorFrete { get; set; }
    public StatusQualificacao QualificacaoNaData { get; set; }
    public DateOnly? PrazoEntregaOfertado { get; set; }
    public string? CondicaoPagamento { get; set; }
    public DateOnly? ValidadeProposta { get; set; }
    public bool TemAnexoProposta { get; set; }
    /// <summary>Fornecedor abriu o convite no portal / salvou rascunho sem enviar.</summary>
    public DateTimeOffset? VisualizadoEm { get; set; }
    public DateTimeOffset? RascunhoEm { get; set; }
    /// <summary>Rascunho salvo no portal (preços, marcas, condições) — só vira proposta no envio.</summary>
    public string? RascunhoJson { get; set; }
    public string? MotivoDeclinio { get; set; }
    /// <summary>Pendência do fornecedor no convite (bloqueado, em qualificação, documento vencido): aparece no convite e no mapa.</summary>
    public string? PendenciaNaData { get; set; }
    public ICollection<PropostaVersao> Versoes { get; set; } = [];
}
