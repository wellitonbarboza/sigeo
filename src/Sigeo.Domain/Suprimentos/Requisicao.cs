using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// T01 · Requisição de compras (material ou serviço). Aprovação em 2 níveis pelo motor de alçadas
/// (Engenheiro da Obra → Diretor Técnico); só vai para cotação depois das duas.
/// As autorizações (T03) são as etapas do motor de aprovação — com nível, decisão e justificativa.
/// </summary>
public class Requisicao : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public string? EnderecoEntrega { get; set; }
    public Guid? CentroCustoId { get; set; }
    public CentroCusto? CentroCusto { get; set; }
    /// <summary>SPE beneficiária (destino do custo). Sai automaticamente dos locais escolhidos; obrigatória quando a obra tem SPEs.</summary>
    public Guid? SpeId { get; set; }
    /// <summary>"Faturar para": estabelecimento que compra (padrão: quem fatura materiais — fábrica de pré-moldados).</summary>
    public Guid? EstabelecimentoFaturamentoId { get; set; }
    /// <summary>Justificativa quando a quantidade passa do orçado para os locais.</summary>
    public string? JustificativaAcimaOrcado { get; set; }
    public TipoRequisicao Tipo { get; set; } = TipoRequisicao.Material;
    /// <summary>Lista CategoriaServico — obrigatória quando TIPO = Serviço.</summary>
    public string? CategoriaServico { get; set; }
    /// <summary>Lista Atividade (pintura, armação, cabeamento…).</summary>
    public string Atividade { get; set; } = string.Empty;
    public DateOnly DataEntrega { get; set; }
    /// <summary>Maior lead time entre os itens (cronograma de suprimentos).</summary>
    public int PrazoCronogramaDias { get; set; }
    public string? Observacao { get; set; }
    public StatusRequisicao Status { get; set; } = StatusRequisicao.Rascunho;
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoEngenheiroPor { get; set; }
    public DateTimeOffset? AprovadoEngenheiroEm { get; set; }
    public string? AprovadoDiretorPor { get; set; }
    public DateTimeOffset? AprovadoDiretorEm { get; set; }
    public string? MotivoReprovacao { get; set; }
    public bool SemOrcamento { get; set; }
    public string? JustificativaSemOrcamento { get; set; }
    public string? ChaveNFe { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string SolicitanteNome { get; set; } = string.Empty;
    /// <summary>Compra emergencial (prazo menor que o lead time): fica fora da base de preço da economia real.</summary>
    public bool Emergencial { get; set; }

    public ICollection<RequisicaoItem> Itens { get; set; } = [];

    public decimal ValorEstimado => Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado).Sum(i => i.Quantidade * (i.ValorReferencia ?? 0));

    /// <summary>Recalcula o prazo do cronograma de suprimentos (maior lead time dos itens).</summary>
    public void RecalcularPrazo() =>
        PrazoCronogramaDias = Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado).Select(i => i.LeadTimeDias).DefaultIfEmpty(0).Max();

    /// <summary>Alerta (não bloqueia) quando a data de entrega não respeita o cronograma de suprimentos.</summary>
    public string? AlertaCronograma(DateOnly hoje) =>
        PrazoCronogramaDias > 0 && DataEntrega < hoje.AddDays(PrazoCronogramaDias)
            ? $"Data de entrega não respeita o cronograma de suprimentos (maior prazo entre os itens: {PrazoCronogramaDias} dias)."
            : null;

    /// <summary>Regras para enviar à aprovação (retorna erros; vazio = pode enviar).</summary>
    public IReadOnlyList<string> ValidarEnvio(bool temAnexoEscopo)
    {
        var e = new List<string>();
        if (ObraId == Guid.Empty) e.Add("Informe a obra / empreendimento.");
        if (string.IsNullOrWhiteSpace(Atividade)) e.Add("Informe a atividade.");
        if (Tipo == TipoRequisicao.Servico)
        {
            if (string.IsNullOrWhiteSpace(CategoriaServico)) e.Add("Requisição de serviço: informe a categoria do serviço.");
            if (!temAnexoEscopo) e.Add("Requisição de serviço: anexe o edital/escopo (projetos, memoriais, ITS).");
        }
        var ativos = Itens.Where(i => i.Status != StatusItemRequisicao.Cancelado).ToList();
        if (ativos.Count == 0) e.Add("Inclua ao menos um item.");
        foreach (var i in ativos)
        {
            if (i.Quantidade <= 0) e.Add($"Item {i.Descricao}: quantidade deve ser maior que zero.");
            if (Tipo == TipoRequisicao.Material && i.ProdutoId is null && string.IsNullOrWhiteSpace(i.Descricao)) e.Add("Item sem produto.");
            if (Tipo == TipoRequisicao.Servico && i.ServicoControladoId is null && string.IsNullOrWhiteSpace(i.Descricao)) e.Add("Item de serviço sem descrição/ITS.");
            if (i.MaterialControlado && string.IsNullOrWhiteSpace(i.EspecificacaoTecnica))
                e.Add($"Item {i.Descricao}: material controlado exige especificação técnica.");
        }
        if (SemOrcamento && string.IsNullOrWhiteSpace(JustificativaSemOrcamento)) e.Add("Compra sem orçamento exige justificativa.");
        return e;
    }
}
