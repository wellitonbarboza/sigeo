using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

public sealed record LinhaSaldo(Guid ItemId, string Descricao, string Unidade, SaldoItem Saldo, string? Lote, string? Comprador, DateOnly? Necessidade, string? MotivoCancelamento);

public sealed record LinhaLote(Guid Id, string Letra, string Categoria, string? Comprador, string? Titular, bool ComSubstituto, SituacaoLote Situacao, DateOnly? Limite, int Itens, string Detalhe);

public sealed record EventoAtendimento(DateTimeOffset Quando, string Tipo, string Texto, string? Link);

public sealed record AtendimentoDto(Guid RequisicaoId, int Numero, StatusRequisicao Status, IReadOnlyList<LinhaSaldo> Itens, IReadOnlyList<LinhaLote> Lotes,
    IReadOnlyList<EventoAtendimento> Linha, decimal ValorRequisitado, decimal ValorPedido, decimal ValorRecebido, decimal ValorAPedir, decimal ValorCancelado, int Pedidos, int Recebimentos);

public sealed record LinhaFila(Guid LoteId, Guid RequisicaoId, int Requisicao, string Letra, string Obra, string Categoria, int Itens, DateOnly? Limite, int DiasAtraso,
    string Situacao, string Origem, bool Substituicao, Guid? CotacaoAberta, string? Comprador);
