using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>Meta de um setor, pessoa ou equipe num período, com alvo, sentido (maior/menor é melhor) e peso na nota.</summary>
public class Meta : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public DonoMeta Dono { get; set; } = DonoMeta.Setor;
    /// <summary>Setor dono (Suprimentos, Engenharia, Qualidade…) quando a meta é do setor.</summary>
    public string? Setor { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public Guid? EquipeId { get; set; }
    public string? EquipeNome { get; set; }
    public Guid? ObraId { get; set; }
    /// <summary>Serviço ligado (ex.: Alvenaria) para o placar por serviço.</summary>
    public string? Servico { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public IndicadorMeta Indicador { get; set; } = IndicadorMeta.Manual;
    public decimal Alvo { get; set; }
    public bool MenorMelhor { get; set; }
    public string Unidade { get; set; } = "%";
    /// <summary>Peso na nota de quem é dono (0 a 100).</summary>
    public decimal Peso { get; set; } = 100;
    public decimal? ValorManual { get; set; }
    public string? Evidencia { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<MetaApuracao> Apuracoes { get; set; } = [];

    public string DonoNome => Dono switch { DonoMeta.Pessoa => UsuarioNome ?? "Pessoa", DonoMeta.Equipe => EquipeNome ?? "Equipe", _ => Setor ?? "Setor" };

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome da meta.");
        if (Fim < Inicio) e.Add("Fim do período antes do início.");
        if (Dono == DonoMeta.Pessoa && UsuarioId is null) e.Add("Meta individual: escolha a pessoa.");
        if (Dono == DonoMeta.Equipe && EquipeId is null) e.Add("Meta de equipe: escolha a equipe.");
        if (Dono == DonoMeta.Setor && string.IsNullOrWhiteSpace(Setor)) e.Add("Meta do setor: informe o setor.");
        if (Alvo <= 0 && !MenorMelhor) e.Add("Informe o alvo.");
        if (Peso is < 0 or > 100) e.Add("Peso entre 0 e 100.");
        return e;
    }
}
