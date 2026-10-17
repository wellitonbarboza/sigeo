using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Sistema;

/// <summary>Solicitação que o usuário pode decidir agora; Prazo vem da alçada da etapa atual.</summary>
public sealed record PendenciaAprovacao(EntidadeAprovavel Entidade, Guid EntidadeId, string Titulo, string Solicitante, decimal Valor, DateTimeOffset Enviado, DateTimeOffset? Prazo, bool Atrasada);

/// <summary>Tudo que a home lê do banco. Delegacao: a delegação vigente do usuário, se houver.</summary>
public sealed record HomeDados(List<PendenciaAprovacao> Aprovar, List<(string Documento, DateOnly Prazo)> Leituras, List<Tarefa> Tarefas, List<SolicitacaoAprovacao> Meus,
    DelegacaoAprovacao? Delegacao, List<(string Texto, int Qtd, string Tom, string Link)> Alertas, bool Vazio);
