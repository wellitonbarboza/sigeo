namespace Groundkey.Application.Abstracoes;

/// <summary>
/// Exclusão genérica de cadastros: confere se o registro está em uso (qualquer chave estrangeira que aponte para ele em
/// registro não excluído) e as regras de situação do documento; se estiver livre, exclui (exclusão lógica, fica na auditoria).
/// Em uso → o usuário inativa em vez de excluir.
/// </summary>
public interface IExclusao
{
    /// <summary>Motivos que impedem excluir (vazio = pode excluir).</summary>
    Task<IReadOnlyList<string>> ImpedimentosAsync(Type tipo, Guid id, CancellationToken ct = default);
    /// <summary>Exclui; lança RegraDeNegocioException com os impedimentos quando não pode.</summary>
    Task ExcluirAsync(Type tipo, Guid id, string? rota, CancellationToken ct = default);
}
