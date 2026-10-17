using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de GRD (guia de remessa de documentos), só leitura.</summary>
public sealed class GrdsConsultas(IFabricaDb fabrica)
{
    /// <summary>Revisões vigentes e contratos ativos para montar a GRD.</summary>
    public async Task<GrdsOpcoes> OpcoesAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var vigentes = await db.Revisoes.AsNoTracking().Where(r => r.Status == StatusRevisao.Vigente || r.Status == StatusRevisao.VigenteComRessalvas)
            .OrderBy(r => r.Documento.Codigo).Select(r => new Opcao(r.Id, r.Documento.Codigo + " R" + r.Revisao.ToString("00") + " – " + r.Documento.Titulo)).ToListAsync(ct);
        var contratos = await db.Contratos.AsNoTracking().Where(k => k.Status == StatusContrato.Ativo).Select(k => new Opcao(k.Id, k.Numero)).ToListAsync(ct);
        return new(vigentes, contratos);
    }

    /// <summary>Razão social dos fornecedores e GRDs com itens, da mais recente.</summary>
    public async Task<GrdsLista> ListarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.RazaoSocial, ct);
        var grds = await db.Grds.AsNoTracking().Include(g => g.Itens).OrderByDescending(g => g.EnviadoEm).ToListAsync(ct);
        return new(forn, grds);
    }

    public async Task<Grd> AbrirAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Grds.AsNoTracking().Include(g => g.Itens).FirstAsync(g => g.Id == id, ct);
    }
}
