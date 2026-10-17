using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Financeiro;

public sealed record LinhaParcelaFin(Guid TituloId, Guid ParcelaId, TipoTitulo Tipo, int Numero, string Parcela, string? Documento, string Pessoa, string Descricao,
    string? Obra, Guid? ObraId, string? Conta, DateOnly Vencimento, decimal Valor, StatusParcelaTitulo Status, DateOnly? DataPagamento, decimal ValorPago,
    string? ContaBancaria, OrigemTitulo Origem, bool Historico, TipoParcelaVenda TipoVenda, int DiasAtraso);

public sealed record BaixaParcela(Guid ParcelaId, DateOnly Data, Guid? ContaBancariaId, FormaPagamento Forma, decimal Correcao, decimal Juros, decimal Multa, decimal Desconto, string? Comprovante);

public sealed record LinhaFluxo(DateOnly Mes, decimal EntradasPrevistas, decimal SaidasPrevistas, decimal EntradasRealizadas, decimal SaidasRealizadas)
{
    public decimal SaldoPrevisto => EntradasPrevistas - SaidasPrevistas;
    public decimal SaldoRealizado => EntradasRealizadas - SaidasRealizadas;
}

public sealed record LinhaDre(string Codigo, string Conta, int Nivel, NaturezaConta Natureza, decimal Valor);

public sealed record LinhaAging(TipoTitulo Tipo, string Faixa, int Parcelas, decimal Valor);

public sealed record SaldoConta(Guid Id, string Conta, decimal Saldo, int NaoConciliados);

public sealed record LinhaMovimento(Guid Id, DateOnly Data, string Historico, decimal Valor, bool Conciliado, bool DeBaixa);
