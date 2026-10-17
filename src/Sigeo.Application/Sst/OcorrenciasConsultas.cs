using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Leituras da tela de ocorrências de SST (Sst/Ocorrencias).</summary>
public sealed class OcorrenciasConsultas(IFabricaDb fabrica)
{
    /// <summary>Ocorrências mais recentes (até <paramref name="limite"/>), nomes de locais, números das ações corretivas e as PTs mais recentes para o seletor.</summary>
    public async Task<OcorrenciasDados> ListarAsync(int limite, int limitePts, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.Ocorrencias.AsNoTracking().OrderByDescending(o => o.DataHora).Take(limite).ToListAsync(ct);
        var locNomes = await db.Locais.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        var acs = await db.AcoesCorretivas.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Numero, ct);
        var pts = await db.PermissoesTrabalho.AsNoTracking().OrderByDescending(p => p.Inicio).Take(limitePts).Select(p => new Opcao(p.Id, p.Numero + " · " + p.Atividade)).ToListAsync(ct);
        return new(itens, locNomes, acs, pts);
    }

    public async Task<Ocorrencia> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Ocorrencias.AsNoTracking().FirstAsync(o => o.Id == id, ct);
    }
}
