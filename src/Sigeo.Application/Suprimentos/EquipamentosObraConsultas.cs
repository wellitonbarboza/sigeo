using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de equipamentos da obra (cadastro, preventivas, OS e famílias).</summary>
public sealed class EquipamentosObraConsultas(IFabricaDb fabrica)
{
    /// <summary>Famílias, situação dos equipamentos (por patrimônio), planos de manutenção e OS mais recentes até o limite.</summary>
    public async Task<EquipamentosObraLista> ListaAsync(DateOnly hoje, int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var familias = await db.FamiliasEquipamento.AsNoTracking().OrderBy(f => f.Nome).ToListAsync(ct);
        var sits = (await EquipamentosServico.SituacoesAsync(db, hoje, ct)).OrderBy(s => s.Equipamento.Patrimonio).ToList();
        var planos = await db.PlanosManutencao.AsNoTracking().ToListAsync(ct);
        var oss = await db.OrdensServico.AsNoTracking().OrderByDescending(o => o.Numero).Take(limite).ToListAsync(ct);
        return new(familias, sits, planos, oss);
    }

    /// <summary>Equipamento com documentos, movimentos e OS (para o histórico).</summary>
    public async Task<EquipamentoObraAberto> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var e = await db.Ativos.AsNoTracking().Include(e => e.Documentos).FirstAsync(e => e.Id == id, ct);
        var movs = await db.MovimentosEquipamento.AsNoTracking().Where(m => m.EquipamentoId == id).ToListAsync(ct);
        var oss = await db.OrdensServico.AsNoTracking().Where(o => o.EquipamentoId == id).ToListAsync(ct);
        return new(e, movs, oss);
    }

    /// <summary>Itens do contrato como opções (descrição, unidade, preço e mensal).</summary>
    public async Task<List<Opcao>> ItensContratoAsync(Guid contratoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await db.Contratos.AsNoTracking().Where(c => c.Id == contratoId).SelectMany(c => c.Itens).ToListAsync(ct))
            .Select(i => new Opcao(i.Id, $"{i.Descricao} ({i.Unidade}) · {i.PrecoUnitario:N2}{(i.PrecoMensal is > 0 ? $" · mês {i.PrecoMensal:N2}" : "")}")).ToList();
    }

    /// <summary>OS aberta e os itens do checklist do plano dela (vazio sem plano).</summary>
    public async Task<OrdemServicoAberta> OrdemServicoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var os = await db.OrdensServico.AsNoTracking().FirstAsync(o => o.Id == id, ct);
        IReadOnlyList<string> itens = os.PlanoId is { } pid ? (await db.PlanosManutencao.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid, ct))?.ItensChecklist ?? [] : [];
        return new(os, itens);
    }
}
