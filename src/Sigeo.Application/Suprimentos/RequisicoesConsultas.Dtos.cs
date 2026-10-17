using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Linha da lista de requisições.</summary>
public sealed record RequisicaoLinha(Guid Id, int Numero, string Obra, TipoRequisicao Tipo, string Atividade, string? CentroCusto, DateOnly Entrega, int Itens, StatusRequisicao Status, string CriadoPor, DateTimeOffset CriadoEm);

/// <summary>Estoque disponível (obra e central) para os itens da requisição.</summary>
public sealed record EstoqueRequisicao(bool ObraPropria, Almoxarifado? Central, Almoxarifado? AlmoxObra, Dictionary<Guid, decimal> SaldoObra, Dictionary<Guid, decimal> SaldoCentral);

/// <summary>Dados de entrega e faturamento da obra da requisição.</summary>
public sealed record ObraEntregaRequisicao(string? EnderecoEntrega, DonoCustoObra DonoCusto, Guid? EstabelecimentoDonoId);

/// <summary>Serviços orçados da obra (para puxar itens do orçamento) e os locais da obra.</summary>
public sealed record OrcamentoRequisicao(int ComposicoesObra, List<ServicoOrcado> Servicos, List<Local> Locais);

/// <summary>Fornecedores para convidar, sugeridos e lotes em compra da requisição.</summary>
public sealed record ConvitesRequisicao(List<Opcao> Fornecedores, HashSet<Guid> Sugeridos, List<LoteCompra> Lotes);
