using QualyHub.Application.Servicos;
using QualyHub.Contratos;
using QualyHub.Web.Client.Servicos;

namespace QualyHub.Web.Servicos;

public sealed class AcessoTelasServidor(AcessoServico acesso) : IAcessoTelas
{
    public Task<MapaAcesso> MapaAsync(CancellationToken ct = default) => acesso.MapaAsync(ct);
}
