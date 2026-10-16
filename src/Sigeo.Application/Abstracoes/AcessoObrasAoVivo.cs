using System.Collections.Concurrent;

namespace Sigeo.Application.Abstracoes;

/// <summary>
/// Obras liberadas por usuário, atualizadas na hora em que o administrador muda o cadastro (o login guarda a foto do
/// momento da entrada; sem isto a mudança só valeria no próximo login). Vale enquanto o processo do app estiver no ar;
/// ao reiniciar, volta a valer o que foi gravado no login.
/// </summary>
public static class AcessoObrasAoVivo
{
    private static readonly ConcurrentDictionary<Guid, (bool Todas, IReadOnlyCollection<Guid> Obras)> Atual = new();

    public static void Definir(Guid usuarioId, bool todas, IEnumerable<Guid> obras) => Atual[usuarioId] = (todas, obras.Distinct().ToList());

    public static bool Obter(Guid usuarioId, out bool todas, out IReadOnlyCollection<Guid> obras)
    {
        if (Atual.TryGetValue(usuarioId, out var v)) { todas = v.Todas; obras = v.Obras; return true; }
        todas = false; obras = []; return false;
    }
}
