using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Cadastros;

public sealed record InsumoAntigo(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo);

public sealed record PreviaInativacao(int Antigos, int ComUso, int Inativar, IReadOnlyList<InsumoAntigo> Amostra);

public sealed record EquivalenteSugerido(Guid Id, string Codigo, string Nome, string Unidade, string? Grupo, string? SubGrupo, decimal Semelhanca, bool MesmaUnidade);

public sealed record ResultadoSubstituicao(string De, string Para, int RequisicoesAtualizadas, int RequisicoesNaoAlteradas, int KitsAtualizados,
    int VinculosImportacao, int ComposicoesComAntigo, int SubstituicoesEncadeadas);
