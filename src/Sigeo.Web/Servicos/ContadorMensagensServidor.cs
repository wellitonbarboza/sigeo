using Sigeo.Application.Comum;
using Sigeo.Web.Client.Servicos;
using Sigeo.Web.Seguranca;

namespace Sigeo.Web.Servicos;

/// <summary>Contador do menu nas telas do servidor: lê o banco e acende na hora quando chega mensagem para o usuário.</summary>
public sealed class ContadorMensagensServidor : IContadorMensagens, IDisposable
{
    private readonly MensagensServico _servico;
    private readonly CanalAvisos _canal;
    private readonly UsuarioAtualWeb _usuario;

    public ContadorMensagensServidor(MensagensServico servico, CanalAvisos canal, UsuarioAtualWeb usuario)
    {
        _servico = servico; _canal = canal; _usuario = usuario;
        _canal.Novidade += AoNovidade;
    }

    public event Action? Mudou;

    public Task<int> NaoLidasAsync(CancellationToken ct = default) => _usuario.UsuarioId is null ? Task.FromResult(0) : _servico.NaoLidasAsync(ct);

    private void AoNovidade(Guid empresa, IReadOnlyCollection<Guid> usuarios)
    {
        if (_usuario.UsuarioId is { } eu && empresa == _usuario.EmpresaId && usuarios.Contains(eu)) Mudou?.Invoke();
    }

    public void Dispose() => _canal.Novidade -= AoNovidade;
}
