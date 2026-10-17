using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da avaliação consolidada de fornecedores, só leitura.</summary>
public sealed class AvaliacaoFornecedoresSgqConsultas(IFabricaDb fabrica)
{
    /// <summary>Parâmetros vigentes (periodicidade e faixas do IDF).</summary>
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    /// <summary>Avaliações do período com o fornecedor, da menor nota.</summary>
    public async Task<List<AvaliacaoSemestral>> ListarAsync(string periodo, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.AvaliacoesSemestrais.AsNoTracking().Include(a => a.Fornecedor).Where(a => a.Periodo == periodo).OrderBy(a => a.NotaMedia).ToListAsync(ct);
    }
}
