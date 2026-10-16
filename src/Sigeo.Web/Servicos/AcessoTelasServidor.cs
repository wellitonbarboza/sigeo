using Sigeo.Application.Servicos;
using Sigeo.Contratos;
using Sigeo.Web.Client.Servicos;

namespace Sigeo.Web.Servicos;

public sealed class AcessoTelasServidor(AcessoServico acesso) : IAcessoTelas
{
    public Task<MapaAcesso> MapaAsync(CancellationToken ct = default) => acesso.MapaAsync(ct);
}
