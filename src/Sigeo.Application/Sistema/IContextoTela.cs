using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

/// <summary>Tela em que o usuário está agindo (rota relativa, ex.: "pedidos"). Null fora de tela (rotinas, login, API sem tela).</summary>
public interface IContextoTela
{
    string? Rota { get; }
}
