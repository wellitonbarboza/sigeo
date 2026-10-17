using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Application.IA;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

public sealed record ItemNotaLido(string Descricao, string? Unidade, decimal Quantidade, decimal? ValorUnitario);

public sealed record PedidoDaNota(Guid Id, int Numero, string Obra);

/// <summary>Resultado da leitura da nota no recebimento: o que foi lido, o que já existe no sistema e avisos para conferir.</summary>
public sealed record NotaLidaNoRecebimento(
    string Chave, string? Numero, string? Serie, string? CnpjEmitente, string? Emitente,
    Guid? FornecedorId, string? Fornecedor,
    Guid? NotaId, SituacaoNota? Situacao, Guid? PedidoDaNotaId,
    IReadOnlyList<PedidoDaNota> PedidosAbertos, int? RecebimentoExistente,
    IReadOnlyList<ItemNotaLido> Itens, string Origem, IReadOnlyList<string> Avisos)
{
    /// <summary>Nota importada (XML/SIEG) já ligada ao pedido: o recebimento sai inteiro dela.</summary>
    public bool ProntaParaReceber => NotaId is not null && PedidoDaNotaId is not null && Situacao == SituacaoNota.Vinculada;
}
