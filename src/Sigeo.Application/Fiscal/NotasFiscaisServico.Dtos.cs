using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

public sealed record ResultadoImportacaoNotas(int Lidas, int Novas, int Repetidas, int Canceladas, int Vinculadas, IReadOnlyList<string> Avisos);
