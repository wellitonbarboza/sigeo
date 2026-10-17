using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;

namespace Sigeo.Application.Cadastros;

public sealed record ResultadoPadronizacao(int Recodificados, int Provisorios, int MantidosPorUso, IReadOnlyList<(string De, string Para, string Produto)> Amostra);
