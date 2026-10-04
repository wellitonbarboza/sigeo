using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Suprimentos;

/// <summary>
/// Transferência do almoxarifado central para a obra/SPE (depois da FVM no recebimento central).
/// Gera o custo na SPE de destino; a obra confere o que chegou (conferência curta).
/// </summary>
public class TransferenciaMaterial : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? RecebimentoId { get; set; }
    public Guid? PedidoId { get; set; }
    /// <summary>Estabelecimento de origem (almoxarifado central / quem faturou).</summary>
    public Guid? OrigemEstabelecimentoId { get; set; }
    /// <summary>SPE de destino (dona do custo).</summary>
    public Guid? SpeId { get; set; }
    public DateOnly Data { get; set; }
    public StatusTransferencia Status { get; set; } = StatusTransferencia.Rascunho;
    public string? ConferidoPor { get; set; }
    public DateTimeOffset? ConferidoEm { get; set; }
    public string? Observacao { get; set; }
    public ICollection<TransferenciaItem> Itens { get; set; } = [];

    public decimal Valor => Itens.Sum(i => i.Valor);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (ObraId == Guid.Empty) e.Add("Informe a obra de destino.");
        if (Itens.Count == 0) e.Add("Inclua os itens transferidos.");
        foreach (var i in Itens.Where(i => i.Quantidade <= 0)) e.Add($"{i.Descricao}: quantidade deve ser maior que zero.");
        return e;
    }

    public void Enviar()
    {
        ValidacaoFalhaSe(Validar());
        if (Status != StatusTransferencia.Rascunho) throw new RegraDeNegocioException("Transferência já enviada.");
        Status = StatusTransferencia.Enviada;
    }

    public void Conferir(string usuario, DateTimeOffset agora, IReadOnlyDictionary<Guid, decimal> recebidas)
    {
        if (Status != StatusTransferencia.Enviada) throw new RegraDeNegocioException("Só transferência enviada pode ser conferida.");
        foreach (var i in Itens) i.QuantidadeRecebida = recebidas.TryGetValue(i.Id, out var q) ? q : i.Quantidade;
        if (Itens.Any(i => i.QuantidadeRecebida != i.Quantidade) && string.IsNullOrWhiteSpace(Observacao))
            throw new RegraDeNegocioException("Quantidade conferida diferente da enviada: descreva a ocorrência na observação.");
        Status = StatusTransferencia.Recebida; ConferidoPor = usuario; ConferidoEm = agora;
    }

    private static void ValidacaoFalhaSe(IReadOnlyList<string> e) { if (e.Count > 0) throw new RegraDeNegocioException(string.Join(" | ", e)); }
}

public class TransferenciaItem : Entidade
{
    public Guid TransferenciaId { get; set; }
    public Guid? ProdutoId { get; set; }
    public Guid? RecebimentoItemId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal? QuantidadeRecebida { get; set; }
    public decimal CustoUnitario { get; set; }
    public string? Lote { get; set; }
    /// <summary>Local de aplicação previsto (opcional).</summary>
    public Guid? LocalId { get; set; }
    public decimal Valor => Math.Round(Quantidade * CustoUnitario, 2);
}
