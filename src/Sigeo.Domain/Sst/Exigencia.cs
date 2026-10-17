using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

/// <summary>S03 · Exigência (documento, ASO, treinamento, EPI). Valores de referência — validar com SST/PGR/PCMSO.</summary>
public class Exigencia : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoExigencia Tipo { get; set; }
    public Guid? TreinamentoId { get; set; }
    public string? CargaInicial { get; set; }
    public int? ValidadeMeses { get; set; }
    public EfeitoVencimento EfeitoVencimento { get; set; } = EfeitoVencimento.BloqueiaPessoa;
    public AtividadeRisco Atividades { get; set; }
    public string Fonte { get; set; } = string.Empty;
    public int Ordem { get; set; }
}
