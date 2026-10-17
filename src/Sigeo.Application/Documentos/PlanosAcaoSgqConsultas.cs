using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de planos de ação de fornecedores, só leitura.</summary>
public sealed class PlanosAcaoSgqConsultas(IFabricaDb fabrica)
{
    /// <summary>Planos com o fornecedor, do mais recente.</summary>
    public async Task<List<PlanoAcaoFornecedor>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.PlanosAcao.Include(p => p.Fornecedor).OrderByDescending(p => p.Numero).ToListAsync(ct);
    }
}
