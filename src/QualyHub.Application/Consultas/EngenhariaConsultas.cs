using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Abstracoes;

namespace QualyHub.Application.Consultas;

/// <summary>
/// Orçamento por local para a tela que roda no navegador: árvore de locais, células serviço × local e composições usadas.
/// Os totais (por serviço, condomínio, torre, apartamento, m²) são somados no navegador — o servidor só lê.
/// </summary>
public sealed class EngenhariaConsultas(IFabricaDb fabrica, PaineisConsultas paineis)
{
    public async Task<OrcamentoObraDto> OrcamentoAsync(Guid obra, CancellationToken ct = default)
    {
        paineis.ConferirObra(obra);
        await using var db = fabrica.CriarLeitura();
        var locais = await db.Locais.AsNoTracking().Where(l => l.ObraId == obra)
            .Select(l => new LocalOrcamento(l.Id, l.LocalPaiId, l.Tipo, l.Nome, l.Codigo, l.Ordem, l.AreaPrivativa, l.TipologiaId)).ToListAsync(ct);
        var cels = await db.ServicosLocais.AsNoTracking().Where(s => s.ObraId == obra)
            .Select(c => new CelulaOrcamento(c.Id, c.ComposicaoId, c.LocalId, c.Quantidade, c.CustoUnitario, c.Custo, c.Travado, c.Origem)).ToListAsync(ct);
        var usadas = cels.Select(c => c.ComposicaoId).Distinct().ToList();
        var comps = await db.Composicoes.AsNoTracking().Where(c => usadas.Contains(c.Id))
            .Select(c => new ComposicaoResumo(c.Id, c.Codigo, c.Descricao, c.Unidade, c.Grupo)).ToListAsync(ct);
        var tips = await db.Tipologias.AsNoTracking().Where(t => t.ObraId == obra).Select(t => new Opcao(t.Id, t.Codigo, null)).ToListAsync(ct);
        return new(locais, cels, comps, tips);
    }
}
