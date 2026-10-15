namespace Groundkey.Tests.Integracao;

/// <summary>
/// Fato que só roda com SQL Server disponível (variável QH_TEST_SQL). Sem a variável o teste é pulado,
/// para o "dotnet test" da solução continuar verde em máquina sem banco.
/// </summary>
public sealed class FatoComBancoAttribute : FactAttribute
{
    public const string Variavel = "QH_TEST_SQL";

    public static string? ConnectionString => Environment.GetEnvironmentVariable(Variavel) is { Length: > 0 } cs ? cs : null;

    public FatoComBancoAttribute()
    {
        if (ConnectionString is null) Skip = $"Defina {Variavel} com a connection string de um SQL Server para rodar os testes de integração.";
    }
}
