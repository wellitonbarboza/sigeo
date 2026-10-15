using Groundkey.Application.Servicos;
using Groundkey.Contratos;
using Groundkey.Web.Client.Servicos;

namespace Groundkey.Web.Servicos;

public sealed class AcessoTelasServidor(AcessoServico acesso) : IAcessoTelas
{
    public Task<MapaAcesso> MapaAsync(CancellationToken ct = default) => acesso.MapaAsync(ct);
}
