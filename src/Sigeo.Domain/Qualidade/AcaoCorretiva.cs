using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q07 · Ação corretiva: causa raiz, plano e eficácia. Reúne uma ou mais RNC (e investigação de acidentes).</summary>
public class AcaoCorretiva : EntidadeAuditavel, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public string? RncsOrigem { get; set; }
    public Guid? OcorrenciaId { get; set; }
    public string Problema { get; set; } = string.Empty;
    public MetodoAnalise MetodoAnalise { get; set; } = MetodoAnalise.CincoPorques;
    public string? AnaliseCausa { get; set; }
    public string? CausaRaiz { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly Prazo { get; set; }
    public DateOnly? DataEficacia { get; set; }
    public string? CriterioEficacia { get; set; }
    public Eficacia ResultadoEficacia { get; set; }
    public bool AtualizaSgq { get; set; }
    public StatusAcaoCorretiva Status { get; set; } = StatusAcaoCorretiva.EmAnalise;
    public ICollection<AcaoCorretivaItem> Itens { get; set; } = [];

    public IReadOnlyList<string> ValidarPlano()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Problema)) e.Add("Descreva o problema.");
        if (string.IsNullOrWhiteSpace(AnaliseCausa)) e.Add("Registre a análise de causa.");
        if (string.IsNullOrWhiteSpace(CausaRaiz)) e.Add("Informe a causa raiz.");
        if (Itens.Count == 0) e.Add("Inclua ao menos uma ação no plano.");
        if (DataEficacia is null) e.Add("Informe a data prevista da verificação de eficácia.");
        if (string.IsNullOrWhiteSpace(CriterioEficacia)) e.Add("Informe o critério de eficácia.");
        return e;
    }

    /// <summary>Avança o status conforme o plano; eficácia registrada encerra (não eficaz → volta à análise).</summary>
    public void Atualizar()
    {
        if (Status == StatusAcaoCorretiva.Encerrada) return;
        if (ResultadoEficacia == Eficacia.NaoEficaz)
        {
            Status = StatusAcaoCorretiva.EmAnalise; ResultadoEficacia = Eficacia.NaoVerificada;
            foreach (var i in Itens) i.Status = StatusItemPlano.Pendente;
            return;
        }
        if (ResultadoEficacia == Eficacia.Eficaz)
        {
            if (Itens.Any(i => i.Status == StatusItemPlano.Pendente)) throw new RegraDeNegocioException("Conclua as ações do plano antes de registrar a eficácia.");
            Status = StatusAcaoCorretiva.Encerrada; return;
        }
        if (Itens.Count > 0 && ValidarPlano().Count == 0)
            Status = Itens.All(i => i.Status != StatusItemPlano.Pendente) ? StatusAcaoCorretiva.AguardandoEficacia : StatusAcaoCorretiva.EmExecucao;
    }
}
