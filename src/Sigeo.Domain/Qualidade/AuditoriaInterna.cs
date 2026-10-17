using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Q10/Q11 — Auditoria interna ═══════════════════════

public class AuditoriaInterna : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public string Escopo { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public string Auditores { get; set; } = string.Empty;
    public string? Independencia { get; set; }
    public string? ResumoAnaliseCritica { get; set; }
    public StatusAuditoria Status { get; set; } = StatusAuditoria.Planejada;
    public ICollection<Constatacao> Constatacoes { get; set; } = [];

    public int Ncs => Constatacoes.Count(c => c.Tipo == TipoConstatacao.NaoConformidade);
    public int Oms => Constatacoes.Count(c => c.Tipo == TipoConstatacao.OportunidadeMelhoria);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Escopo)) e.Add("Informe o escopo.");
        if (string.IsNullOrWhiteSpace(Criterio)) e.Add("Informe o critério.");
        if (string.IsNullOrWhiteSpace(Auditores)) e.Add("Informe o(s) auditor(es).");
        if (Fim < Inicio) e.Add("Fim anterior ao início.");
        if (string.IsNullOrWhiteSpace(Independencia)) e.Add("Registre a independência (auditor não audita o próprio trabalho).");
        return e;
    }

    public void EmitirRelatorio()
    {
        if (Constatacoes.Count == 0) throw new RegraDeNegocioException("Inclua o checklist.");
        if (Constatacoes.Any(c => c.Tipo == TipoConstatacao.NaoAvaliado)) throw new RegraDeNegocioException("Registre o resultado de todos os itens do checklist.");
        if (Constatacoes.Any(c => c.Tipo != TipoConstatacao.Conforme && string.IsNullOrWhiteSpace(c.Evidencia)))
            throw new RegraDeNegocioException("Constatação diferente de Conforme exige evidência.");
        Status = StatusAuditoria.RelatorioEmitido;
    }
}
