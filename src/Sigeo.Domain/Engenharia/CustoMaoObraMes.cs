using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Custo mensal de mão de obra própria (por equipe, colaborador ou função): salário, encargos, benefícios e outros.
/// Pode ser replicado do mês anterior e ajustado. Base do custo-hora real da apropriação.
/// </summary>
public class CustoMaoObraMes : EntidadeDaObra
{
    /// <summary>1º dia do mês de competência.</summary>
    public DateOnly Competencia { get; set; }
    public Guid? EquipeId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? FuncaoObraId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Pessoas { get; set; } = 1;
    /// <summary>Salário base por pessoa.</summary>
    public decimal Salario { get; set; }
    public decimal EncargosPerc { get; set; }
    /// <summary>Benefícios por pessoa (VT, VA, cesta, plano…).</summary>
    public decimal Beneficios { get; set; }
    /// <summary>Outros custos do mês (horas extras, alojamento, EPI…).</summary>
    public decimal Outros { get; set; }
    public decimal HorasMes { get; set; } = 220;

    public decimal Total => Math.Round(Pessoas * (Salario * (1 + EncargosPerc / 100m) + Beneficios) + Outros, 2);
    /// <summary>Custo-hora teórico (total ÷ horas disponíveis).</summary>
    public decimal CustoHora => Pessoas * HorasMes <= 0 ? 0 : Math.Round(Total / (Pessoas * HorasMes), 4);

    public static DateOnly Mes(DateOnly d) => new(d.Year, d.Month, 1);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (EquipeId is null && ColaboradorId is null && FuncaoObraId is null && string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a equipe, o colaborador, a função ou a descrição.");
        if (Pessoas < 1) e.Add("Pessoas deve ser pelo menos 1.");
        if (Salario < 0 || Beneficios < 0 || Outros < 0) e.Add("Valores não podem ser negativos.");
        if (EncargosPerc is < 0 or > 300) e.Add("Encargos entre 0 e 300%.");
        if (HorasMes <= 0) e.Add("Horas no mês devem ser maiores que zero.");
        return e;
    }

    /// <summary>Cópia para outra competência (replicar o mês anterior).</summary>
    public CustoMaoObraMes CopiarPara(DateOnly competencia) => new()
    {
        ObraId = ObraId, Competencia = Mes(competencia), EquipeId = EquipeId, ColaboradorId = ColaboradorId, FuncaoObraId = FuncaoObraId, Descricao = Descricao,
        Pessoas = Pessoas, Salario = Salario, EncargosPerc = EncargosPerc, Beneficios = Beneficios, Outros = Outros, HorasMes = HorasMes
    };
}
