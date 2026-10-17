using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Sistema;

public sealed record ResultadoLogin(Usuario? Usuario, string? EmpresaNome, bool TrocarSenha, string? Erro);
