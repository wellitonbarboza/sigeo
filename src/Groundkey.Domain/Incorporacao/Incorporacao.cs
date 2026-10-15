using Groundkey.Domain.Common;
using Groundkey.Domain.Financeiro;

namespace Groundkey.Domain.Incorporacao;

public enum RegimeTributario { RetGeral = 1, RetSocial = 2, LucroPresumido = 3, LucroReal = 4 }
public enum SituacaoUnidade { Disponivel = 1, Reservada = 2, Proposta = 3, Vendida = 4, Permuta = 5, Bloqueada = 6 }
public enum StatusContratoVenda { Ativo = 1, Quitado = 2, Distratado = 3 }
public enum FormaComissao { AVista = 1, Parcelada = 2, AposPercentualPago = 3 }

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

/// <summary>Alíquotas por regime e vigência (a reforma tributária muda a divisão sem mexer no código: cadastra-se nova vigência).</summary>
public class AliquotaRegime : EntidadeAuditavel
{
    public RegimeTributario Regime { get; set; }
    public DateOnly VigenciaInicio { get; set; }
    public decimal Irpj { get; set; }
    public decimal Csll { get; set; }
    public decimal Pis { get; set; }
    public decimal Cofins { get; set; }
    public decimal Cbs { get; set; }
    public decimal Ibs { get; set; }
    public string? CodigoReceita { get; set; }
    public string? Observacao { get; set; }
    public decimal Total => Irpj + Csll + Pis + Cofins + Cbs + Ibs;
}

/// <summary>Variação mensal de índice (INCC, IPCA, IGP-M…) em %, informada pelo financeiro.</summary>
public class IndiceEconomico : EntidadeAuditavel
{
    public string Indice { get; set; } = "INCC";
    public DateOnly Competencia { get; set; }
    public decimal VariacaoPerc { get; set; }
}

/// <summary>Unidade à venda: o apartamento/casa (Local) com preço de tabela e situação comercial.</summary>
public class UnidadeComercial : EntidadeDaObra
{
    public Guid LocalId { get; set; }
    public decimal PrecoTabela { get; set; }
    public SituacaoUnidade Situacao { get; set; } = SituacaoUnidade.Disponivel;
    public string? Vagas { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Comprador (dados pessoais protegidos: só quem tem a tela de vendas vê).</summary>
public class Comprador : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string? Conjuge { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Contrato de compra e venda da unidade: condição em blocos (entrada, mensais, balões, chaves, financiamento, permuta) e comissões.</summary>
public class ContratoVenda : EntidadeDaObra
{
    public int Numero { get; set; }
    public Guid UnidadeComercialId { get; set; }
    public Guid CompradorId { get; set; }
    public DateOnly DataContrato { get; set; }
    /// <summary>Data-base dos valores (a correção conta a partir dela).</summary>
    public DateOnly DataBase { get; set; }
    public decimal ValorVenda { get; set; }
    public string IndiceAteChaves { get; set; } = "INCC";
    public string IndiceAposChaves { get; set; } = "IPCA";
    public decimal JurosAposChavesMes { get; set; }
    public DateOnly? DataChaves { get; set; }
    public StatusContratoVenda Status { get; set; } = StatusContratoVenda.Ativo;
    public Guid? TituloId { get; set; }
    public DateOnly? DataDistrato { get; set; }
    public decimal? ValorDevolvido { get; set; }
    public string? Observacao { get; set; }
    public ICollection<BlocoCondicaoVenda> Blocos { get; set; } = [];
    public ICollection<ComissaoVenda> Comissoes { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (UnidadeComercialId == Guid.Empty) e.Add("Escolha a unidade.");
        if (CompradorId == Guid.Empty) e.Add("Escolha o comprador.");
        if (ValorVenda <= 0) e.Add("Informe o valor da venda.");
        if (DataContrato == default) e.Add("Informe a data do contrato.");
        if (Blocos.Count == 0) e.Add("Monte a condição de pagamento (entrada, mensais, balões, chaves…).");
        foreach (var b in Blocos)
        {
            if (b.Quantidade <= 0 || b.ValorParcela <= 0) e.Add($"{NomesVenda.Bloco(b.Tipo)}: quantidade e valor devem ser maiores que zero.");
            if (b.Tipo == TipoParcelaVenda.Permuta && b.FornecedorPermutaId is null) e.Add("Permuta: escolha o fornecedor que entrega em troca.");
        }
        var soma = Blocos.Sum(b => b.Total);
        if (Blocos.Count > 0 && Math.Abs(soma - ValorVenda) > 0.05m) e.Add($"A condição soma {soma.ToString("N2", Groundkey.Domain.Financeiro.Cultura.Br)}, diferente do valor da venda ({ValorVenda.ToString("N2", Groundkey.Domain.Financeiro.Cultura.Br)}).");
        foreach (var c in Comissoes)
        {
            if (c.FornecedorId == Guid.Empty) e.Add("Comissão: escolha o corretor/imobiliária (cadastro de fornecedores).");
            if (c.Valor <= 0) e.Add("Comissão: informe o percentual ou o valor.");
            if (c.Forma == FormaComissao.Parcelada && c.Parcelas < 2) e.Add("Comissão parcelada: informe 2 parcelas ou mais.");
            if (c.Forma == FormaComissao.AposPercentualPago && c.PercentualPagoMinimo is not (> 0 and <= 100)) e.Add("Comissão após % pago: informe o percentual (1 a 100).");
        }
        return e;
    }
}

/// <summary>Bloco da condição de venda: N parcelas de mesmo valor a cada M meses a partir do 1º vencimento.</summary>
public class BlocoCondicaoVenda : Entidade
{
    public Guid ContratoVendaId { get; set; }
    public TipoParcelaVenda Tipo { get; set; } = TipoParcelaVenda.Mensal;
    public int Quantidade { get; set; } = 1;
    public decimal ValorParcela { get; set; }
    public DateOnly PrimeiroVencimento { get; set; }
    /// <summary>Meses entre parcelas (1 mensal, 6 semestral, 12 anual).</summary>
    public int IntervaloMeses { get; set; } = 1;
    public Guid? FornecedorPermutaId { get; set; }
    public decimal Total => Quantidade * ValorParcela;

    public IEnumerable<(DateOnly Vencimento, decimal Valor)> Parcelas() =>
        Enumerable.Range(0, Math.Max(Quantidade, 0)).Select(i => (PrimeiroVencimento.AddMonths(i * Math.Max(IntervaloMeses, 1)), ValorParcela));
}

/// <summary>Comissão da venda (corretor, imobiliária, gerente). Vira título a pagar: à vista, parcelada ou quando o cliente pagar X%.</summary>
public class ComissaoVenda : Entidade
{
    public Guid ContratoVendaId { get; set; }
    public Guid FornecedorId { get; set; }
    public string Papel { get; set; } = "Corretor";
    public decimal Percentual { get; set; }
    public decimal Valor { get; set; }
    public FormaComissao Forma { get; set; } = FormaComissao.AVista;
    public int Parcelas { get; set; } = 1;
    public decimal? PercentualPagoMinimo { get; set; }
    public Guid? TituloId { get; set; }
    public bool Estornada { get; set; }
}

/// <summary>Apuração mensal do RET da incorporação (regime de caixa: receita recebida no mês).</summary>
public class ApuracaoRet : EntidadeAuditavel
{
    public Guid IncorporacaoId { get; set; }
    public Guid ObraId { get; set; }
    public DateOnly Competencia { get; set; }
    public RegimeTributario Regime { get; set; }
    public decimal ReceitaRecebida { get; set; }
    public decimal Irpj { get; set; }
    public decimal Csll { get; set; }
    public decimal Pis { get; set; }
    public decimal Cofins { get; set; }
    public decimal Cbs { get; set; }
    public decimal Ibs { get; set; }
    public decimal AliquotaTotal { get; set; }
    public Guid? TituloId { get; set; }
    public decimal Total => Irpj + Csll + Pis + Cofins + Cbs + Ibs;
}

public static class NomesVenda
{
    public static string Bloco(TipoParcelaVenda t) => t switch
    {
        TipoParcelaVenda.Entrada => "Entrada", TipoParcelaVenda.Mensal => "Mensais", TipoParcelaVenda.Intermediaria => "Balões (intermediárias)",
        TipoParcelaVenda.Chaves => "Chaves", TipoParcelaVenda.Financiamento => "Financiamento bancário", TipoParcelaVenda.Permuta => "Permuta", _ => "Avulsa"
    };

    public static string Situacao(SituacaoUnidade s) => s switch
    {
        SituacaoUnidade.Disponivel => "Disponível", SituacaoUnidade.Reservada => "Reservada", SituacaoUnidade.Proposta => "Em proposta",
        SituacaoUnidade.Vendida => "Vendida", SituacaoUnidade.Permuta => "Permuta", _ => "Bloqueada"
    };

    public static string Regime(RegimeTributario r) => r switch
    {
        RegimeTributario.RetGeral => "RET 4%", RegimeTributario.RetSocial => "RET social 1%", RegimeTributario.LucroPresumido => "Lucro presumido", _ => "Lucro real"
    };
}

/// <summary>
/// Correção monetária das parcelas de venda: índice "até as chaves" (INCC) e "após as chaves" (IPCA), mês a mês desde a data-base
/// até o mês anterior ao do vencimento (defasagem de 1 mês, prática de mercado). Após as chaves soma os juros do contrato.
/// Mês sem índice informado conta 0% (o financeiro vê o aviso).
/// </summary>
public static class MotorCorrecao
{
    public sealed record Resultado(decimal Fator, decimal ValorCorrigido, decimal Correcao, decimal Juros, IReadOnlyList<DateOnly> MesesSemIndice);

    public static Resultado Corrigir(decimal valor, DateOnly dataBase, DateOnly vencimento, DateOnly? chaves, string indiceAntes, string indiceDepois,
        decimal jurosMes, IReadOnlyDictionary<(string Indice, DateOnly Mes), decimal> indices)
    {
        var fator = 1m; var fatorJuros = 1m;
        var faltam = new List<DateOnly>();
        var mes = new DateOnly(dataBase.Year, dataBase.Month, 1);
        var ate = new DateOnly(vencimento.Year, vencimento.Month, 1).AddMonths(-1);
        while (mes <= ate)
        {
            var depois = chaves is { } c && mes >= new DateOnly(c.Year, c.Month, 1);
            var idx = depois ? indiceDepois : indiceAntes;
            if (indices.TryGetValue((idx, mes), out var v)) fator *= 1 + v / 100m;
            else faltam.Add(mes);
            if (depois && jurosMes > 0) fatorJuros *= 1 + jurosMes / 100m;
            mes = mes.AddMonths(1);
        }
        var corrigido = Math.Round(valor * fator, 2);
        var comJuros = Math.Round(corrigido * fatorJuros, 2);
        return new Resultado(Math.Round(fator, 8), comJuros, corrigido - valor, comJuros - corrigido, faltam);
    }
}
