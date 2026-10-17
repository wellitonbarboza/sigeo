using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

/// <summary>XMLs de NF-e baixados de uma fonte externa (SIEG) para um CNPJ e período.</summary>
public interface IFonteNotas
{
    /// <summary>Credenciais da fonte cadastradas para a empresa.</summary>
    bool Configurada(Guid empresaId);
    /// <summary>XMLs (NF-e e eventos) das notas emitidas contra o CNPJ no período (até 2 meses por chamada).</summary>
    Task<IReadOnlyList<(string Nome, string Xml)>> BaixarAsync(Guid empresaId, string cnpjDestinatario, DateOnly de, DateOnly ate, CancellationToken ct = default);
}
