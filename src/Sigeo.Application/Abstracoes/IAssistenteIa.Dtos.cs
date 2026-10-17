namespace Sigeo.Application.Abstracoes;

/// <summary>Resposta do modelo com o consumo (para o controle de custo por empresa).</summary>
public sealed record RespostaIa(string Texto, int TokensEntrada, int TokensSaida);
