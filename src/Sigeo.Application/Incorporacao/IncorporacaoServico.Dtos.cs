using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Incorporacao;

namespace Sigeo.Application.Incorporacao;

public sealed record LinhaUnidade(Guid Id, Guid LocalId, string Unidade, string? Torre, string? Pavimento, int Ordem, string? Tipologia, decimal? Area,
    decimal PrecoTabela, SituacaoUnidade Situacao, string? Comprador, Guid? ContratoId, decimal? ValorVenda, int OrdemPavimento = 0);

public sealed record LinhaContratoVenda(Guid Id, int Numero, string Unidade, string Comprador, DateOnly Data, decimal Valor, StatusContratoVenda Status,
    decimal Recebido, decimal AReceber, int Atrasadas);

public sealed record LinhaCarteira(Guid ParcelaId, Guid TituloId, Guid ContratoId, int Contrato, string Unidade, string Comprador, TipoParcelaVenda Tipo,
    int Numero, DateOnly Vencimento, decimal ValorBase, decimal ValorCorrigido, StatusParcelaTitulo Status, DateOnly? DataPagamento, decimal ValorPago, int DiasAtraso);

public sealed record PainelVendas(decimal Vgv, decimal VgvVendido, decimal VgvEstoque, int Unidades, int Vendidas, int Disponiveis,
    decimal Recebido, decimal AReceber, decimal Inadimplente, IReadOnlyList<(DateOnly Mes, int Vendas, decimal Valor)> Vso,
    IReadOnlyList<(DateOnly Mes, decimal Valor)> ProjecaoRecebimentos, IReadOnlyList<DateOnly> MesesSemIndice);

public sealed record CalculoParcela(decimal ValorBase, decimal Correcao, decimal Juros, decimal Total, IReadOnlyList<DateOnly> MesesSemIndice);
