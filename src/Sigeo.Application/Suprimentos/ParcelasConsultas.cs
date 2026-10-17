using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Leituras da tela de parcelas de contratos recorrentes (locação e serviço contínuo).</summary>
public sealed class ParcelasConsultas(IFabricaDb fabrica)
{
    /// <summary>Contratos recorrentes e parcelas não canceladas (de um contrato, quando informado), até o limite.</summary>
    public async Task<ParcelasLista> ListaAsync(Guid? contratoId, int limite, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var contratos = await db.Contratos.AsNoTracking().Where(c => c.Recorrente && c.FormaCobranca != FormaCobranca.Medicao).OrderBy(c => c.Numero)
            .Select(c => new ContratoRecorrenteResumo(c.Id, c.Numero, c.Fornecedor.RazaoSocial, c.FormaCobranca, c.ToleranciaPerc)).ToListAsync(ct);
        var porId = contratos.ToDictionary(c => c.Id);
        var q = db.ParcelasContrato.AsNoTracking().Where(p => p.Status != StatusParcela.Cancelada);
        if (contratoId is { } id) q = q.Where(p => p.ContratoId == id);
        var lista = await q.OrderBy(p => p.Vencimento).ThenBy(p => p.Numero).Take(limite).ToListAsync(ct);
        var linhas = lista.Where(p => porId.ContainsKey(p.ContratoId)).Select(p =>
        {
            var c = porId[p.ContratoId];
            return new ParcelaLinha(p, c.Numero, c.Fornecedor, p.ForaDaTolerancia(c.Tolerancia));
        }).ToList();
        return new(contratos, linhas);
    }

    /// <summary>Parcela aberta com o contrato (fornecedor e itens) e, na cobrança por uso, os usos do período.</summary>
    public async Task<ParcelaAberta> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var p = await db.ParcelasContrato.AsNoTracking().FirstAsync(p => p.Id == id, ct);
        var k = await db.Contratos.AsNoTracking().Include(c => c.Fornecedor).Include(c => c.Itens).FirstAsync(c => c.Id == p.ContratoId, ct);
        List<UsoContrato> usos = k.FormaCobranca == FormaCobranca.Uso
            ? await db.UsosContrato.AsNoTracking().Where(u => u.ContratoId == k.Id && u.Data >= p.Inicio && u.Data <= p.Fim).OrderBy(u => u.Data).ToListAsync(ct)
            : [];
        return new(p, k, usos);
    }

    /// <summary>Itens do contrato como opções para lançar um uso.</summary>
    public async Task<List<Opcao>> ItensContratoAsync(Guid? contratoId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return (await db.ContratoItens.AsNoTracking().Where(i => i.ContratoId == contratoId).OrderBy(i => i.Item).ToListAsync(ct))
            .Select(i => new Opcao(i.Id, $"{i.Descricao} ({i.Unidade}) · {i.PrecoUnitario:N2}")).ToList();
    }
}
