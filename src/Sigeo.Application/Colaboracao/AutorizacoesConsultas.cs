using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Colaboracao;

/// <summary>Leitura do componente de trilha de aprovação (Shared/Autorizacoes).</summary>
public sealed class AutorizacoesConsultas(IFabricaDb fabrica, AprovacaoServico aprov)
{
    /// <summary>Última solicitação do registro, alçadas da entidade e se o usuário pode decidir a etapa pendente.</summary>
    public async Task<AutorizacoesDados> CarregarAsync(EntidadeAprovavel entidade, Guid entidadeId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var s = await AprovacaoServico.UltimaAsync(db, entidade, entidadeId, ct);
        var alcadas = await db.Alcadas.Where(a => a.Entidade == entidade).ToListAsync(ct);
        var pode = s is not null && await aprov.PodeDecidirAsync(db, s, ct);
        return new(s, alcadas, pode);
    }
}
