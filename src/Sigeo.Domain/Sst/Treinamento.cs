using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S05/S06/S07 — Treinamentos ═══════════════════════

public class Treinamento : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public TipoTreinamento Tipo { get; set; } = TipoTreinamento.NR;
    public Guid? DocumentoId { get; set; }
    public string CargaHoraria { get; set; } = string.Empty;
    public string? Conteudo { get; set; }
    public int? ValidadeMeses { get; set; }
    public MetodoEficaciaTreinamento MetodoEficacia { get; set; } = MetodoEficaciaTreinamento.Prova;
    public bool Ativo { get; set; } = true;
}
