using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Lançamento de custo realizado da obra (material, serviço, equipamento, frete…) por fase × local.
/// Vem do recebimento (material), da medição aprovada (serviço), da importação do histórico do ERP ou é lançado à mão.
/// A fase é o "Grupo de serviço" da EAP (o mesmo das composições), o que permite o orçado × realizado por fase.
/// </summary>
public class CustoRealizado : EntidadeDaObra
{
    public const string FaseAApropriar = "A apropriar";

    public DateOnly Data { get; set; }
    public TipoCusto TipoCusto { get; set; } = TipoCusto.Material;
    /// <summary>Grupo de serviço (fase da EAP de custo). "A apropriar" quando a origem não diz.</summary>
    public string Fase { get; set; } = FaseAApropriar;
    /// <summary>Local (torre/pavimento/unidade). Vazio = custo da obra como um todo (canteiro, administração…).</summary>
    public Guid? LocalId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public Guid? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal Valor { get; set; }
    /// <summary>Tipo do documento (NF, recibo, medição, fatura…).</summary>
    public string? DocumentoTipo { get; set; }
    public string? DocumentoNumero { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? RecebimentoId { get; set; }
    public Guid? MedicaoId { get; set; }
    public OrigemCusto Origem { get; set; } = OrigemCusto.Manual;
    /// <summary>Chave do registro de origem (evita lançar duas vezes a mesma linha numa reimportação ou reprocessamento).</summary>
    public string? ChaveOrigem { get; set; }
    /// <summary>Lançado antes da data de corte (histórico do ERP): só leitura de valor; fase e local podem ser reclassificados.</summary>
    public bool Historico { get; set; }
    public Guid? LoteImportacaoId { get; set; }
    public string? Observacao { get; set; }

    public decimal? ValorUnitario => Quantidade == 0 ? null : Math.Round(Valor / Quantidade, 4);

    /// <summary>Valor e quantidade vêm de outro registro (recebimento, medição, ERP): aqui só se reclassifica fase e local.</summary>
    public bool ValorTravado => Origem != OrigemCusto.Manual;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (ObraId == Guid.Empty) e.Add("Selecione a obra.");
        if (Data == default) e.Add("Informe a data.");
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Informe a descrição.");
        if (string.IsNullOrWhiteSpace(Fase)) e.Add("Informe a fase (grupo de serviço).");
        if (Valor == 0) e.Add("Informe o valor.");
        if (Quantidade < 0) e.Add("Quantidade não pode ser negativa.");
        return e;
    }
}
