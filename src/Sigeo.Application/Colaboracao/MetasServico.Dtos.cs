using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Planejamento;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Colaboracao;

public sealed record LinhaMeta(Meta Meta, decimal? Valor, decimal? Atingido, string Situacao, string Tom, int Tarefas, int TarefasConcluidas);

public sealed record LinhaPlacar(string Chave, string Nome, int Metas, int MetasAtingidas, int TarefasNoPrazo, int TarefasTotal, int Atrasadas, decimal? Nota);

public sealed record LinhaTarefaMeta(Guid Id, int Numero, string Titulo, string? Meta, string Responsavel, string? Equipe, DateOnly Prazo, DateOnly PrazoOriginal,
    StatusTarefa Status, AceiteTarefa Aceite, bool Atrasada, string? Motivo);

public sealed record MetasDto(IReadOnlyList<LinhaMeta> Metas, IReadOnlyList<LinhaPlacar> PorPessoa, IReadOnlyList<LinhaPlacar> PorEquipe, IReadOnlyList<LinhaPlacar> PorServico,
    IReadOnlyList<LinhaTarefaMeta> Tarefas);
