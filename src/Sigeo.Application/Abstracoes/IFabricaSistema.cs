using Microsoft.EntityFrameworkCore;
namespace Sigeo.Application.Abstracoes;

/// <summary>Contextos com identidade de sistema (portal externo, QR, carga padrão, rotinas) — sempre auditados.</summary>
public interface IFabricaSistema
{
    ISigeoDb Criar(Guid empresaId, string origem);
}
