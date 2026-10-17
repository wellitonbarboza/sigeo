using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S09/S10 — Inspeções de segurança ═══════════════════════

public class InspecaoSeguranca : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public Guid ModeloInspecaoId { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public Guid LocalId { get; set; }
    public string InspetorNome { get; set; } = string.Empty;
    public DateTimeOffset Data { get; set; }
    public decimal? Conformidade { get; set; }
    public string? Paralisacao { get; set; }
    public StatusInspecaoSst Status { get; set; } = StatusInspecaoSst.EmAndamento;
    public ICollection<InspecaoSegurancaItem> Itens { get; set; } = [];

    public IReadOnlyList<string> Concluir()
    {
        var e = new List<string>();
        if (Itens.Any(i => i.Resultado == ResultadoItem.NaoAvaliado)) e.Add("Avalie todos os itens.");
        foreach (var i in Itens.Where(i => i.Resultado == ResultadoItem.NaoConforme))
        {
            if (i.Gravidade is null) e.Add($"Item {i.Ordem}: informe a gravidade.");
            if (string.IsNullOrWhiteSpace(i.Responsavel)) e.Add($"Item {i.Ordem}: informe o responsável / empresa.");
            if (i.Prazo is null) e.Add($"Item {i.Ordem}: informe o prazo.");
        }
        if (Itens.Any(i => i.Gravidade == Gravidade.RiscoGraveIminente) && string.IsNullOrWhiteSpace(Paralisacao))
            e.Add("Risco grave e iminente: registre a paralisação da frente (NR-03).");
        if (e.Count > 0) return e;
        var avaliados = Itens.Count(i => i.Resultado != ResultadoItem.NaoAplica);
        Conformidade = avaliados == 0 ? null : Math.Round(100m * Itens.Count(i => i.Resultado == ResultadoItem.Conforme) / avaliados, 1);
        Status = Itens.Any(i => i.Resultado == ResultadoItem.NaoConforme && !i.Corrigido) ? StatusInspecaoSst.EmTratamento : StatusInspecaoSst.Concluida;
        return e;
    }
}
