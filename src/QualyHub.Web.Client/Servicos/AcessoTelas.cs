using QualyHub.Contratos;

namespace QualyHub.Web.Client.Servicos;

/// <summary>
/// Acesso do usuário por tela (menu, bloqueio por endereço, botões). O layout serve às telas do servidor e às do navegador:
/// no servidor lê direto; no navegador pergunta à API. Mudança no perfil vale na próxima navegação, sem sair e entrar.
/// </summary>
public interface IAcessoTelas
{
    Task<MapaAcesso> MapaAsync(CancellationToken ct = default);
}

public sealed class AcessoTelasApi(Api api) : IAcessoTelas
{
    public Task<MapaAcesso> MapaAsync(CancellationToken ct = default) => api.ObterAsync<MapaAcesso>("api/telas/acesso", ct);
}
