using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

public sealed class SemContextoTela : IContextoTela
{
    public string? Rota => null;
}
