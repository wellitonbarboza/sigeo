using Microsoft.EntityFrameworkCore;

namespace Sigeo.Application.Sistema;

/// <summary>Leituras da tela de usuários, alçadas e delegações (Sistema/Usuarios).</summary>
public sealed class UsuariosConsultas(IFabricaDb fabrica)
{
    public async Task<UsuariosDados> CarregarAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var usuarios = await db.Usuarios.Include(u => u.Obras).Include(u => u.LimitesAprovacao).OrderBy(u => u.Nome).ToListAsync(ct);
        var alcadas = await db.Alcadas.OrderBy(a => a.Entidade).ThenBy(a => a.Ordem).ToListAsync(ct);
        var delegacoes = await db.DelegacoesAprovacao.OrderByDescending(d => d.Inicio).ToListAsync(ct);
        return new(usuarios, alcadas, delegacoes);
    }
}
