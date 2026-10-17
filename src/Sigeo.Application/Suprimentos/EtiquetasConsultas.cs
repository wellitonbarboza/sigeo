using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da impressão de etiquetas (equipamentos da obra ou instrumentos de aferição).</summary>
public sealed class EtiquetasConsultas(IFabricaDb fabrica)
{
    /// <summary>
    /// Parâmetros da folha de etiquetas e os itens pedidos: com <paramref name="afericao"/>, instrumentos e nome do fornecedor dono;
    /// senão, equipamentos com documentos e nome das famílias.
    /// </summary>
    public async Task<EtiquetasDados> DadosAsync(List<Guid> ids, bool afericao, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        if (afericao)
        {
            var itens = await db.Equipamentos.AsNoTracking().Where(e => ids.Contains(e.Id)).OrderBy(e => e.Codigo).ToListAsync(ct);
            var forn = itens.Where(e => e.FornecedorId != null).Select(e => e.FornecedorId!.Value).Distinct().ToList();
            var nomes = await db.Fornecedores.AsNoTracking().Where(f => forn.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NomeFantasia ?? f.RazaoSocial, ct);
            return new(p, itens, nomes, [], []);
        }
        var ativos = await db.Ativos.AsNoTracking().Include(e => e.Documentos).Where(e => ids.Contains(e.Id)).OrderBy(e => e.Patrimonio).ToListAsync(ct);
        var fams = await db.FamiliasEquipamento.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.Nome, ct);
        return new(p, [], [], ativos, fams);
    }
}
