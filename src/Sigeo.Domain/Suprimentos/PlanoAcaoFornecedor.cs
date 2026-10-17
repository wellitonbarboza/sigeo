using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T16 · Plano de ação do fornecedor (aberto por nota de medição, FVM reprovada, avaliação semestral ou reclamação).</summary>
public class PlanoAcaoFornecedor : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Ex.: PA-0087</summary>
    public string Numero { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid? ObraId { get; set; }
    public OrigemPlanoAcao Origem { get; set; }
    public Guid? OrigemId { get; set; }
    public TipoPlanoAcao TipoPlano { get; set; }
    public string Problema { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateOnly Prazo { get; set; }
    public StatusPlanoAcao Status { get; set; } = StatusPlanoAcao.Aberta;
    public Eficacia Eficacia { get; set; }
    public string? EficaciaComentario { get; set; }

    public void Concluir(Eficacia eficacia, string? comentario)
    {
        if (eficacia == Eficacia.NaoVerificada) throw new Aprovacoes.RegraDeNegocioException("Registre a verificação de eficácia para concluir.");
        Eficacia = eficacia;
        EficaciaComentario = comentario;
        Status = StatusPlanoAcao.Concluida;
    }
}
