using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Serviço do orçamento com a sua composição (insumos por unidade de serviço). Ex.: contrapiso convencional (m²) =
/// cimento + areia + aditivo + pedreiro/servente (horas) ou empreitada (R$/m²). Liga-se à ITS (serviço controlado → FVS).
/// </summary>
public class Composicao : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    /// <summary>Unidade do serviço (m², m, m³, un, vb).</summary>
    public string Unidade { get; set; } = "m²";
    /// <summary>Etapa / grupo da EAP (lista "Grupo de serviço").</summary>
    public string? Grupo { get; set; }
    /// <summary>Disciplina (lista configurável: civil, hidráulica, elétrica…) — usada nos kits por apartamento.</summary>
    public string? Disciplina { get; set; }
    public RegraQuantidade Regra { get; set; } = RegraQuantidade.AreaPiso;
    /// <summary>Multiplicador sobre a medida do local (ex.: rodapé = perímetro × 0,9 para descontar portas).</summary>
    public decimal Fator { get; set; } = 1;
    /// <summary>ITS / serviço controlado (puxa o modelo de FVS e a sigla do mapa).</summary>
    public Guid? ServicoControladoId { get; set; }
    public bool Ativa { get; set; } = true;
    public string? Observacao { get; set; }
    public ICollection<ComposicaoItem> Itens { get; set; } = [];

    public decimal CustoUnitario => Math.Round(Itens.Sum(i => i.CustoPorUnidade), 4);
    public decimal Custo(TipoInsumo t) => Math.Round(Itens.Where(i => i.Tipo == t).Sum(i => i.CustoPorUnidade), 4);
    /// <summary>Horas de mão de obra própria por unidade de serviço (base da apropriação).</summary>
    public decimal HorasPorUnidade => Itens.Where(i => i.Tipo == TipoInsumo.MaoObraPropria).Sum(i => i.Coeficiente);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Codigo)) e.Add("Informe o código do serviço.");
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a descrição do serviço.");
        if (string.IsNullOrWhiteSpace(Unidade)) e.Add("Informe a unidade.");
        if (Fator <= 0) e.Add("Fator deve ser maior que zero.");
        foreach (var i in Itens)
        {
            if (i.Coeficiente <= 0) e.Add($"{i.Descricao}: coeficiente deve ser maior que zero.");
            if (i.PerdaPerc is < 0 or > 100) e.Add($"{i.Descricao}: perda entre 0 e 100%.");
            if (i.CustoUnitario < 0) e.Add($"{i.Descricao}: custo não pode ser negativo.");
            if (i.Tipo == TipoInsumo.Material && i.ProdutoId is null) e.Add($"{i.Descricao}: material precisa do produto do cadastro.");
            if (i.Tipo == TipoInsumo.MaoObraPropria && i.FuncaoObraId is null) e.Add($"{i.Descricao}: mão de obra própria precisa da função (base da apropriação em horas).");
            if (string.IsNullOrWhiteSpace(i.Descricao)) e.Add("Insumo sem descrição.");
        }
        return e;
    }
}
