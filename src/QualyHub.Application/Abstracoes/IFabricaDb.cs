namespace QualyHub.Application.Abstracoes;

/// <summary>
/// Cria um contexto curto por operação (padrão do Blazor Server: o circuito vive muito, o contexto não — e não é thread-safe).
/// </summary>
public interface IFabricaDb
{
    /// <summary>Contexto para gravar (rastreia o que for carregado).</summary>
    IQualyHubDb Criar();

    /// <summary>Contexto só de leitura: nada é rastreado (sem snapshot por linha). Use em listagens e consultas de tela.</summary>
    IQualyHubDb CriarLeitura();

    /// <summary>Usuário da operação (perfil, empresa, obras permitidas).</summary>
    IUsuarioAtual Usuario { get; }

    /// <summary>
    /// Várias gravações numa transação, compatível com a nova tentativa do Azure SQL: cada tentativa usa um contexto novo
    /// (nada fica meio salvo) e só confirma se tudo der certo.
    /// </summary>
    Task EmTransacaoAsync(Func<IQualyHubDb, Task> acao, CancellationToken ct = default);
}

/// <summary>Contextos com identidade de sistema (portal externo, QR, carga padrão, rotinas) — sempre auditados.</summary>
public interface IFabricaSistema
{
    IQualyHubDb Criar(Guid empresaId, string origem);
}
