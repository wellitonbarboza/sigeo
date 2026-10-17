using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>Célula da matriz serviço × local: quantidade e custo orçados. Travada = editada à mão (o gerador não mexe).</summary>
public class ServicoLocal : EntidadeDaObra
{
    public Guid ComposicaoId { get; set; }
    public Guid LocalId { get; set; }
    public Guid? SpeId { get; set; }
    public decimal Quantidade { get; set; }
    /// <summary>Custo unitário da composição na data da geração (fotografia).</summary>
    public decimal CustoUnitario { get; set; }
    public decimal Custo { get; set; }
    public string Origem { get; set; } = OrigemCelula.Tipologia;
    public bool Travado { get; set; }
}
