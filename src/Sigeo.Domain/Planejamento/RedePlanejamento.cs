using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Rede de planejamento (conceito de redes por locais): conjunto de locais + pacotes de serviço em sequência.
/// Torre por apartamento, estrutura por pavimento, área comum por local. O cronograma é gerado a partir das redes.
/// </summary>
public class RedePlanejamento : EntidadeDaObra
{
    public string Nome { get; set; } = string.Empty;
    public TipoRede Tipo { get; set; } = TipoRede.PorApartamento;
    public string Cor { get; set; } = "#4f52d6";
    /// <summary>Pulmão (dias úteis) entre um pacote e o seguinte no mesmo local.</summary>
    public int Pulmao { get; set; }
    public int Ordem { get; set; }
    public ICollection<RedeLocal> Locais { get; set; } = [];
    public ICollection<RedePacote> Pacotes { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome da rede.");
        if (Pulmao is < 0 or > 60) e.Add("Pulmão entre 0 e 60 dias úteis.");
        if (Locais.Count == 0) e.Add("Inclua ao menos um local na rede.");
        if (Pacotes.Count == 0) e.Add("Inclua ao menos um pacote de serviço.");
        foreach (var p in Pacotes)
        {
            if (string.IsNullOrWhiteSpace(p.Nome)) e.Add("Pacote sem nome.");
            if (p.DuracaoPorLocal is < 0 or > 400) e.Add($"{p.Nome}: duração por local entre 0 e 400 dias úteis.");
            if (p.Equipes is < 1 or > 50) e.Add($"{p.Nome}: equipes entre 1 e 50.");
        }
        return e;
    }
}
