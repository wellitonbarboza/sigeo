using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de FVS (fichas de verificação de serviço), só leitura.</summary>
public sealed class FvssConsultas(IFabricaDb fabrica)
{
    /// <summary>Meta de aprovação na 1ª inspeção e equipamentos usáveis na data.</summary>
    public async Task<FvssInicio> InicioAsync(DateOnly hoje, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var meta = (await ParametrosServico.CarregarAsync(db, ct))[Parametros.MetaFvs1aInspecaoPerc];
        var equipamentos = (await db.Equipamentos.AsNoTracking().ToListAsync(ct)).Where(e => e.UsavelEm(hoje)).Select(e => new Opcao(e.Id, $"{e.Codigo} – {e.Descricao} (até {e.Proxima:dd/MM/yy})")).ToList();
        return new(meta, equipamentos);
    }

    /// <summary>FVS mais recentes (até o limite) com os nomes de locais e fornecedores para montar as linhas.</summary>
    public async Task<FvssLista> ListarAsync(int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var locais = await db.Locais.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome, ct);
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, ct);
        var lista = await db.Fvss.AsNoTracking().Include(f => f.Itens).Include(f => f.Modelo).OrderByDescending(f => f.DataInspecao).Take(limite).ToListAsync(ct);
        return new(locais, forn, lista);
    }

    public async Task<Local?> LocalAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Locais.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <summary>
    /// FVS com itens, modelo, local e RNC. O equipamento da FVS só é lido quando não está entre os já listados
    /// (<paramref name="equipamentosListados"/>): a tela o mostra como fora da validade.
    /// </summary>
    public async Task<FvsAberta> AbrirAsync(Guid id, IReadOnlyCollection<Guid> equipamentosListados, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var f = await db.Fvss.AsNoTracking().Include(f => f.Itens).Include(f => f.Modelo).Include(f => f.Local).FirstAsync(f => f.Id == id, ct);
        var rnc = f.RncId is { } r ? await db.Rncs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r, ct) : null;
        EquipamentoMedicao? fora = null;
        if (f.EquipamentoId is { } eq && equipamentosListados.All(x => x != eq))
            fora = await db.Equipamentos.AsNoTracking().FirstAsync(x => x.Id == eq, ct);
        return new(f, rnc, fora);
    }
}
