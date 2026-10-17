using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Diário de obra (RDO): um por obra e dia. Preenche e assina o engenheiro(a) da obra; depois de assinado não muda.
/// Efetivo próprio por função (do controle de mão de obra) e de terceiros por nome do empreiteiro, ambos com quantidade.
/// Atividades em linhas, com local opcional e fotos (anexos da atividade).
/// </summary>
public class DiarioObra : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public DateOnly Data { get; set; }
    public ClimaDia ClimaManha { get; set; }
    public ClimaDia ClimaTarde { get; set; }
    public CondicaoDia Condicao { get; set; }
    public DiaTrabalhado Trabalhado { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraTermino { get; set; }
    public string? ObservacaoClima { get; set; }
    public string? Ocorrencias { get; set; }
    public string? Visitas { get; set; }
    public string? Equipamentos { get; set; }
    public StatusDiario Status { get; set; } = StatusDiario.Rascunho;
    public Guid? AssinadoPorId { get; set; }
    public string? AssinadoPorNome { get; set; }
    public DateTimeOffset? AssinadoEm { get; set; }
    public ICollection<DiarioEfetivo> Efetivo { get; set; } = [];
    public ICollection<DiarioAtividade> Atividades { get; set; } = [];
    /// <summary>Equipamentos do dia (horas trabalhadas alimentam o horímetro; parado aparece no painel).</summary>
    public ICollection<DiarioEquipamento> EquipamentosDia { get; set; } = [];

    public bool Editavel => Status == StatusDiario.Rascunho;
    public int TotalProprio => Efetivo.Where(e => !e.Terceiro).Sum(e => e.Quantidade);
    public int TotalTerceiros => Efetivo.Where(e => e.Terceiro).Sum(e => e.Quantidade);

    public IReadOnlyList<string> Validar(DateOnly hoje)
    {
        var e = new List<string>();
        if (Data == default) e.Add("Informe a data do diário.");
        else if (Data > hoje) e.Add("O diário não pode ser de data futura.");
        if (HoraInicio is { } i && HoraTermino is { } t && t <= i) e.Add("O término do expediente deve ser depois do início.");
        foreach (var x in Efetivo)
        {
            if (string.IsNullOrWhiteSpace(x.Nome)) e.Add(x.Terceiro ? "Efetivo de terceiros: informe o nome do terceiro." : "Efetivo próprio: escolha a função.");
            if (x.Quantidade is < 0 or > 2000) e.Add($"{x.Nome}: quantidade entre 0 e 2000.");
        }
        foreach (var dup in Efetivo.GroupBy(x => (x.Terceiro, Nome: x.Nome.Trim().ToUpperInvariant())).Where(g => g.Count() > 1 && g.Key.Nome.Length > 0))
            e.Add($"{dup.First().Nome} aparece mais de uma vez no efetivo {(dup.Key.Terceiro ? "de terceiros" : "próprio")}.");
        foreach (var a in Atividades.Where(a => string.IsNullOrWhiteSpace(a.Descricao))) e.Add($"Atividade {a.Ordem}: descreva o que foi feito.");
        return e;
    }

    public void Assinar(Guid usuarioId, string nome, DateTimeOffset agora)
    {
        if (!Editavel) throw new Aprovacoes.RegraDeNegocioException("O diário já está assinado.");
        Status = StatusDiario.Assinado; AssinadoPorId = usuarioId; AssinadoPorNome = nome; AssinadoEm = agora;
    }
}
