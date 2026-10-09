namespace QualyHub.Application.Comum;

/// <summary>
/// Avisos em tempo real dentro do mesmo servidor: mensagem nova acende o contador de quem está com o QualyHub aberto
/// (Blazor Server) e acorda a rotina de e-mail para enviar na hora. Com mais de uma instância do App Service,
/// quem está na outra instância vê no próximo ciclo (o contador também consulta a cada minuto).
/// </summary>
public sealed class CanalAvisos
{
    private readonly SemaphoreSlim _acordar = new(0, 1);

    /// <summary>Empresa e usuários com novidade na caixa de mensagens.</summary>
    public event Action<Guid, IReadOnlyCollection<Guid>>? Novidade;

    public void Publicar(Guid empresaId, IReadOnlyCollection<Guid> usuarios, bool acordarEnvio = true)
    {
        try { Novidade?.Invoke(empresaId, usuarios); } catch { /* assinante com erro não derruba o envio */ }
        if (acordarEnvio) Acordar();
    }

    /// <summary>Pede para a rotina de envio processar a fila agora (sem esperar o próximo ciclo).</summary>
    public void Acordar()
    {
        if (_acordar.CurrentCount == 0) try { _acordar.Release(); } catch (SemaphoreFullException) { }
    }

    /// <summary>Espera o próximo pedido de envio ou o tempo máximo, o que vier primeiro. True = foi acordada.</summary>
    public async Task<bool> EsperarAsync(TimeSpan maximo, CancellationToken ct)
    {
        try { return await _acordar.WaitAsync(maximo, ct); }
        catch (OperationCanceledException) { return false; }
    }
}
