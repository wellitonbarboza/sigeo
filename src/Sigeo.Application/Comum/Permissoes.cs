using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Comum;

/// <summary>Checagem de perfil no serviço (a tela esconde o botão; o serviço garante a regra).</summary>
public static class Permissoes
{
    public static async Task ExigirAsync(ISigeoDb db, IUsuarioAtual usuario, string acao, params Domain.Common.PerfilUsuario[] perfis)
    {
        if (usuario.UsuarioId is not { } id) return; // rotinas de sistema/importação
        var p = await db.Usuarios.AsNoTracking().Where(u => u.Id == id).Select(u => (Domain.Common.PerfilUsuario?)u.Perfil).FirstOrDefaultAsync();
        if (p is Domain.Common.PerfilUsuario.Administrador || (p is { } x && perfis.Contains(x))) return;
        throw new Domain.Aprovacoes.RegraDeNegocioException($"Seu perfil não permite {acao}.");
    }

    /// <summary>Confere a ação na tela pelo perfil de acesso do usuário (telas liberadas para a construtora + perfil + exceções).</summary>
    public static async Task ExigirTelaAsync(ISigeoDb db, IUsuarioAtual usuario, string rota, char acao, CancellationToken ct = default)
    {
        if (usuario.UsuarioId is not { } id) return;
        Sistema.AcessoServico.Conferir(await Sistema.AcessoServico.CalcularAsync(db, id, ct), rota, acao);
    }
}
