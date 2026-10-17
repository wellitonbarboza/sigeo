using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

/// <summary>S04 · Documento/ASO da pessoa. ASO guarda só conclusão, aptidões e validade — nunca diagnóstico.</summary>
public class DocumentoColaborador : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid ColaboradorId { get; set; }
    public Guid ExigenciaId { get; set; }
    public DateOnly DataEmissao { get; set; }
    public DateOnly? Validade { get; set; }
    public ConclusaoAso? ConclusaoAso { get; set; }
    public AtividadeRisco AptoAtividades { get; set; }
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNome { get; set; }
    public bool ArquivoRestrito { get; set; }
    public OrigemDocColaborador Origem { get; set; } = OrigemDocColaborador.Sst;
    public ValidacaoDoc Validacao { get; set; } = ValidacaoDoc.Aguardando;
    public string? MotivoRecusa { get; set; }
    public string? ValidadoPor { get; set; }
    public DateTimeOffset? ValidadoEm { get; set; }

    public IReadOnlyList<string> Validar(Exigencia ex)
    {
        var e = new List<string>();
        if (ex.ValidadeMeses is not null && Validade is null) e.Add("Informe a validade.");
        if (Validade is { } v && v < DataEmissao) e.Add("Validade anterior à emissão.");
        if (ex.Tipo == TipoExigencia.ExameAso && ConclusaoAso is null) e.Add("ASO: informe a conclusão (apto/inapto).");
        if (ex.Tipo == TipoExigencia.ExameAso) ArquivoRestrito = true;
        return e;
    }

    public void Decidir(bool valido, string? motivo, string por, DateTimeOffset agora)
    {
        if (!valido && string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo da recusa (vai para a empresa).");
        Validacao = valido ? ValidacaoDoc.Validado : ValidacaoDoc.Recusado;
        MotivoRecusa = valido ? null : motivo; ValidadoPor = por; ValidadoEm = agora;
    }
}
