using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de regras de retenção de registros, só leitura.</summary>
public sealed class RegistrosConsultas(IFabricaDb fabrica)
{
    public async Task<List<RegraRetencao>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.RegrasRetencao.OrderBy(r => r.Identificacao).ToListAsync(ct);
    }
}
