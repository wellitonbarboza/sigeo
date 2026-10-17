using Sigeo.Domain.Common;
using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

public sealed record ColaboradoresLista(List<Colaborador> Ativos, List<Colaborador> Todos);

/// <summary>Ficha do colaborador numa leitura só.</summary>
public sealed record ColaboradorFicha(Colaborador Colaborador, List<DocumentoColaborador> Documentos,
    List<(string Treinamento, string Turma, DateTimeOffset Data, ResultadoParticipante? Resultado, DateOnly? ValidoAte)> Treinamentos,
    List<(string Epi, decimal Qtd, DateOnly Data, DateOnly? Troca, string Assinatura)> Epis);
