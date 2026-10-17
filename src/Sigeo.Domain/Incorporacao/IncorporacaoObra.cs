using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Dados da incorporação da obra: memorial, patrimônio de afetação, regime tributário (RET) e indexação padrão das vendas.</summary>
public class IncorporacaoObra : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
    public Guid? SpeId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? MemorialRegistro { get; set; }
    public DateOnly? DataRegistroMemorial { get; set; }
    public bool PatrimonioAfetacao { get; set; }
    public DateOnly? DataAverbacao { get; set; }
    /// <summary>CNPJ próprio da incorporação afetada (exigido pela Receita para o RET).</summary>
    public string? CnpjIncorporacao { get; set; }
    public RegimeTributario Regime { get; set; } = RegimeTributario.RetGeral;
    public DateOnly? DataOpcaoRet { get; set; }
    /// <summary>Opção pelo recolhimento unificado de IBS/CBS na transição (exige pedido do RET antes de 01/01/2029).</summary>
    public bool OpcaoTransicaoIbsCbs { get; set; }
    public DateOnly? PrevisaoChaves { get; set; }
    public string IndiceAteChaves { get; set; } = "INCC";
    public string IndiceAposChaves { get; set; } = "IPCA";
    /// <summary>Juros após as chaves (% ao mês, Tabela Price simples sobre o saldo corrigido).</summary>
    public decimal JurosAposChavesMes { get; set; } = 1m;
    /// <summary>Dia de vencimento do DARF do RET no mês seguinte.</summary>
    public int DiaVencimentoRet { get; set; } = 20;
}
