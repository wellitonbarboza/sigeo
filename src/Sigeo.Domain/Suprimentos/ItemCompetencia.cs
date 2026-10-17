using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class ItemCompetencia : Entidade
{
    public Guid CompetenciaId { get; set; }
    public Guid TipoDocumentoExigidoId { get; set; }
    /// <summary>Documento por funcionário (holerite, ponto, EPI…); nulo = documento da empresa.</summary>
    public Guid? ColaboradorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool PodeAdiar { get; set; }
    public SituacaoItemCompetencia Situacao { get; set; } = SituacaoItemCompetencia.Pendente;
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    /// <summary>Documento do fornecedor gerado pelo envio (documentos da empresa), para o cadastro e relatórios.</summary>
    public Guid? FornecedorDocumentoId { get; set; }
    public string? MotivoRecusa { get; set; }
    public string? ConferidoPor { get; set; }
    public DateTimeOffset? ConferidoEm { get; set; }
    public string? JustificativaAdiamento { get; set; }
    public DateOnly? AdiadoAte { get; set; }
    public string? AdiamentoAprovadoPor { get; set; }
    public DateTimeOffset? AdiamentoAprovadoEm { get; set; }

    public void Enviado(string blob, string nome, string quem, DateTimeOffset agora)
    {
        if (Situacao == SituacaoItemCompetencia.Aprovado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: já aprovado.");
        ArquivoBlob = blob; ArquivoNome = nome; EnviadoPor = quem; EnviadoEm = agora; MotivoRecusa = null;
        Situacao = SituacaoItemCompetencia.Enviado;
    }

    public void Conferir(bool aprovado, string? motivo, string quem, DateTimeOffset agora)
    {
        if (Situacao is not (SituacaoItemCompetencia.Enviado or SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Recusado))
            throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: nada enviado para conferir.");
        if (!aprovado && string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: informe o motivo da recusa.");
        Situacao = aprovado ? SituacaoItemCompetencia.Aprovado : SituacaoItemCompetencia.Recusado;
        MotivoRecusa = aprovado ? null : motivo!.Trim();
        ConferidoPor = quem; ConferidoEm = agora;
    }

    public void SolicitarAdiamento(string? justificativa, DateOnly? ate, DateOnly hoje)
    {
        if (!PodeAdiar) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: este documento não pode ser adiado.");
        if (Situacao is SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Enviado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: já enviado.");
        if (string.IsNullOrWhiteSpace(justificativa)) throw new Aprovacoes.RegraDeNegocioException("Informe a justificativa do adiamento.");
        if (ate is not { } d || d <= hoje) throw new Aprovacoes.RegraDeNegocioException("Informe a nova data de entrega (futura).");
        if (d > hoje.AddDays(60)) throw new Aprovacoes.RegraDeNegocioException("Adiamento de no máximo 60 dias.");
        JustificativaAdiamento = justificativa.Trim(); AdiadoAte = d;
        Situacao = SituacaoItemCompetencia.AdiamentoSolicitado;
        AdiamentoAprovadoPor = null; AdiamentoAprovadoEm = null;
    }

    /// <summary>Engenheiro da obra aprova (item fica "Adiado até" e não trava a medição) ou recusa (volta a pendente).</summary>
    public void DecidirAdiamento(bool aprovado, string? motivo, string quem, DateTimeOffset agora)
    {
        if (Situacao != SituacaoItemCompetencia.AdiamentoSolicitado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: não há pedido de adiamento.");
        if (!aprovado && string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Informe o motivo da recusa do adiamento.");
        if (aprovado) { Situacao = SituacaoItemCompetencia.Adiado; AdiamentoAprovadoPor = quem; AdiamentoAprovadoEm = agora; }
        else { Situacao = SituacaoItemCompetencia.Pendente; MotivoRecusa = $"Adiamento recusado: {motivo!.Trim()}"; AdiadoAte = null; }
    }

    /// <summary>Adiado e passou da nova data sem entrega: volta a travar a próxima medição.</summary>
    public bool AdiamentoVencido(DateOnly hoje) => Situacao == SituacaoItemCompetencia.Adiado && AdiadoAte is { } a && a < hoje;
}
