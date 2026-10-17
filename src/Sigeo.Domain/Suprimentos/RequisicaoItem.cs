using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T02 · Item da requisição (carrega especificação técnica e marca material controlado).</summary>
public class RequisicaoItem : Entidade
{
    public Guid RequisicaoId { get; set; }
    public Requisicao Requisicao { get; set; } = null!;
    public Guid? ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public bool MesmaMarca { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal Quantidade { get; set; }
    public decimal? ValorReferencia { get; set; }
    public int LeadTimeDias { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
    public bool MaterialControlado { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControladoRef { get; set; }
    public string? EspecificacaoTecnica { get; set; }
    public string? DocumentosExigidos { get; set; }
    public DateOnly? DataNecessidade { get; set; }
    public StatusItemRequisicao Status { get; set; } = StatusItemRequisicao.Aberto;
    public string? Observacao { get; set; }
    /// <summary>Saldo encerrado (cancelado) com justificativa — não será mais pedido.</summary>
    public decimal QtdCancelada { get; set; }
    public string? MotivoCancelamento { get; set; }
    /// <summary>Lote de compra (comprador) a que o item foi encaminhado na autorização.</summary>
    public Guid? LoteCompraId { get; set; }
    /// <summary>Serviço do orçamento de origem (serviço contratado ou material puxado da composição).</summary>
    public Guid? ComposicaoId { get; set; }
    /// <summary>Quebra por local (serviço × local × quantidade) quando o item vem do orçamento.</summary>
    public ICollection<RequisicaoItemLocal> Locais { get; set; } = [];

    /// <summary>Herda do produto: unidade, lead time e marcação de material controlado (com modelo de especificação e documentos exigidos).</summary>
    public void HerdarDoProduto(Produto p)
    {
        ProdutoId = p.Id;
        Produto = p;
        Descricao = p.Nome;
        Unidade = p.Unidade;
        LeadTimeDias = p.LeadTimeDias;
        MaterialControladoId = p.MaterialControladoId;
        MaterialControlado = p.MaterialControladoId is not null;
        if (p.MaterialControlado is { } mc)
        {
            MaterialControladoRef = mc;
            EspecificacaoTecnica ??= mc.EspecificacaoModelo;
            DocumentosExigidos ??= mc.DocumentosExigidos;
        }
    }
}
