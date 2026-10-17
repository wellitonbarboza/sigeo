using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>Insumo da composição: material (produto do cadastro), mão de obra própria (horas da função), empreitada, equipamento ou terceiro.</summary>
public class ComposicaoItem : Entidade
{
    public Guid ComposicaoId { get; set; }
    public int Ordem { get; set; }
    public TipoInsumo Tipo { get; set; } = TipoInsumo.Material;
    public Guid? ProdutoId { get; set; }
    /// <summary>Mão de obra própria: função (pedreiro, servente…).</summary>
    public Guid? FuncaoObraId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    /// <summary>Quantidade do insumo por unidade do serviço.</summary>
    public decimal Coeficiente { get; set; }
    public decimal PerdaPerc { get; set; }
    public decimal CustoUnitario { get; set; }

    public decimal QuantidadePor(decimal qtdServico) => Math.Round(qtdServico * Coeficiente * (1 + PerdaPerc / 100m), 4);
    public decimal CustoPorUnidade => Math.Round(Coeficiente * (1 + PerdaPerc / 100m) * CustoUnitario, 4);
}
