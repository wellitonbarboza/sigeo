using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T12 · Item do contrato (planilha de preços — base da medição).</summary>
public class ContratoItem : Entidade
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    /// <summary>Ex.: 1.1</summary>
    public string Item { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
    public string Unidade { get; set; } = "un";
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    /// <summary>Locação por diária: valor mensal (a diária é cobrada até o mensal ficar mais barato).</summary>
    public decimal? PrecoMensal { get; set; }
    public decimal Total => Math.Round(Quantidade * PrecoUnitario, 2);
    /// <summary>Serviço do orçamento contratado.</summary>
    public Guid? ComposicaoId { get; set; }
    /// <summary>Quantidade contratada por local (medição por local).</summary>
    public ICollection<ContratoItemLocal> Locais { get; set; } = [];
}
