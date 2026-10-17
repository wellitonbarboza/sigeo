using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Leituras da tela de cópias controladas e solicitações de cópia, só leitura.</summary>
public sealed class CopiasConsultas(IFabricaDb fabrica)
{
    /// <summary>Cópias (já em linhas da grade), solicitações com itens e revisões vigentes para solicitar.</summary>
    public async Task<CopiasDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var copias = await db.Copias.AsNoTracking().OrderByDescending(c => c.CriadoEm)
            .Select(c => new CopiaLinha(c.Id, c.NumeroCopia, c.Revisao.Documento.Codigo + " – " + c.Revisao.Documento.Titulo, "R" + c.Revisao.Revisao.ToString("00"),
                c.TipoCopia, c.Destinatario, c.EntregueEm, c.PrazoRecolhimento, c.Status, c.QrToken)).ToListAsync(ct);
        var solic = await db.SolicitacoesCopia.AsNoTracking().Include(s => s.Itens).OrderByDescending(s => s.Numero).ToListAsync(ct);
        var vigentes = await db.Revisoes.AsNoTracking().Where(r => r.Status == StatusRevisao.Vigente || r.Status == StatusRevisao.VigenteComRessalvas)
            .OrderBy(r => r.Documento.Codigo).Select(r => new Opcao(r.Id, r.Documento.Codigo + " R" + r.Revisao.ToString("00") + " – " + r.Documento.Titulo)).ToListAsync(ct);
        return new(copias, solic, vigentes);
    }
}
