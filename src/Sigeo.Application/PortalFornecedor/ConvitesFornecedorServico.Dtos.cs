using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>Contato do fornecedor visto pela equipe interna (sem máscara), com os canais possíveis.</summary>
public sealed record ContatoConvite(Guid Id, string Nome, string? Email, string? Celular, bool Principal);
