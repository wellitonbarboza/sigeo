using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Qualidade;

/// <summary>Leituras da tela de equipamentos de medição (aferição) e seus tipos (só leitura).</summary>
public sealed class EquipamentosMedicaoConsultas(IFabricaDb fabrica)
{
    /// <summary>Parâmetros vigentes (alerta de calibração e periodicidade padrão).</summary>
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct);
    }

    /// <summary>Equipamentos por código e tipos de medição por nome.</summary>
    public async Task<EquipamentosMedicaoLista> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var itens = await db.Equipamentos.AsNoTracking().OrderBy(e => e.Codigo).ToListAsync(ct);
        var tipos = await db.TiposEquipamentoMedicao.AsNoTracking().OrderBy(t => t.Nome).ToListAsync(ct);
        return new(itens, tipos);
    }

    /// <summary>Equipamento com o histórico de calibrações e as últimas 20 cautelas.</summary>
    public async Task<EquipamentoMedicaoAberto> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var e = await db.Equipamentos.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var cals = await db.Calibracoes.AsNoTracking().Where(c => c.EquipamentoMedicaoId == id).OrderByDescending(c => c.Data).ToListAsync(ct);
        var cautelas = await db.CautelasFerramenta.AsNoTracking().Where(c => c.EquipamentoMedicaoId == id).OrderByDescending(c => c.Data).ThenByDescending(c => c.CriadoEm).Take(20).ToListAsync(ct);
        return new(e, cals, cautelas);
    }
}
