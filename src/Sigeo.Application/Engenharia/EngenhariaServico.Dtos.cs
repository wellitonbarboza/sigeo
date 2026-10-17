using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Engenharia;

public sealed record LinhaResumoCondominio(Guid QuadraId, string Condominio, string? Spe, string? Cnpj, int Torres, int Pavimentos, int Apartamentos, int Comodos, int AreasComuns,
    decimal AreaPrivativa, decimal FracaoSoma, IReadOnlyList<(string Tipologia, int Unidades, decimal Area)> PorTipologia);

public sealed record ResultadoSincronizacao(int Atualizados, int Criados);
