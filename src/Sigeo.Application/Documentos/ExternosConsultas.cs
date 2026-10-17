using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de documentos externos (normas e legislação), só leitura.</summary>
public sealed class ExternosConsultas(IFabricaDb fabrica)
{
    /// <summary>Periodicidade de verificação dos externos, em meses (parâmetro).</summary>
    public async Task<int> MesesVerificacaoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await ParametrosServico.CarregarAsync(db, ct)).Int(Parametros.VerificacaoExternosMeses);
    }

    public async Task<List<DocumentoExterno>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.DocumentosExternos.OrderBy(e => e.Identificacao).ToListAsync(ct);
    }
}
