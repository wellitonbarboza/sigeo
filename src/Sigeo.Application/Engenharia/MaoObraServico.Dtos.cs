using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Engenharia;

namespace Sigeo.Application.Engenharia;

public sealed record FechamentoMes(DateOnly Competencia, decimal CustoTotal, decimal HorasApropriadas, decimal CustoSemApropriacao,
    IReadOnlyList<FechamentoMaoObra.CustoEquipe> Equipes, IReadOnlyList<FechamentoMaoObra.CustoCelula> Celulas);
