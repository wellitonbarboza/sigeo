using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

public sealed record MetaComprador(Guid? UsuarioId, string Nome, decimal? Economia, decimal? MetaEconomia, decimal? TempoCompra, decimal? MetaTempo,
    decimal? NoPrazo, decimal? MetaNoPrazo, int LotesAbertos, int Atrasados, bool Ausente);

public sealed record LeadCategoria(string Categoria, decimal? Cadastrado, decimal? Real, decimal? DentroLeadPerc, decimal? AteNecessidadePerc, int Itens);

public sealed record NecessidadeSemPedido(Guid RequisicaoId, int Requisicao, string Item, DateOnly Necessidade, decimal Quantidade, string Unidade, string? Comprador);

public sealed record EntregaFornecedor(Guid FornecedorId, string Fornecedor, int Pedidos, int NoPrazo, decimal Perc);

public sealed record AlertaFornecedor(Guid FornecedorId, string Fornecedor, string Tipo, string Detalhe, string Tom, int PedidosAbertos, decimal ValorAberto);

public sealed record NcRecebimento(Guid RecebimentoId, DateOnly Data, string Fornecedor, string Itens, string Obra, string Tipo, string Tratamento);

public sealed record VolumeRequisicoes(int Recebidas, int Atendidas, int Parciais, int EncerradasComSaldo, int EmCompra, decimal ValorAtendido, decimal SaldoParcial);

public sealed record PainelSuprimentosDto(IReadOnlyList<MetaComprador> Compradores, MetaComprador Setor, VolumeRequisicoes Volume, decimal? AteNecessidadePerc, decimal? MetaNecessidade,
    IReadOnlyList<LeadCategoria> LeadTime, IReadOnlyList<NecessidadeSemPedido> Proximos7Dias, IReadOnlyList<EntregaFornecedor> Entregas, IReadOnlyList<AlertaFornecedor> Alertas,
    IReadOnlyList<NcRecebimento> NaoConformidades);
