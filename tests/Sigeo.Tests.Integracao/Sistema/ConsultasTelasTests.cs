using Sigeo.Domain.Aprovacoes;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Sigeo.Tests.Integracao;

/// <summary>
/// Leituras das telas (*Consultas) no banco real: toda classe monta pelo DI e todo método roda contra o SQL Server
/// com argumentos neutros (obra fictícia, ids vazios, hoje). Pega LINQ que o EF não traduz, coluna errada e dependência
/// faltando antes de alguém abrir a tela. "Não encontrado" (First em lista vazia, regra de negócio) é resultado aceito.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ConsultasTelasTests(BancoFixture f)
{
    private static readonly Assembly Application = typeof(AprovacaoServico).Assembly;

    private static IEnumerable<Type> Classes(string sufixo) => Application.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.Name.EndsWith(sufixo, StringComparison.Ordinal)
                    && t.GetConstructors().Length > 0);

    private ServiceProvider Montar()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IUsuarioAtual>(f.Usuario);
        sc.AddSingleton(f.Relogio);
        sc.AddSingleton(f.Fabrica);
        sc.AddSingleton(new ProtecaoCpf(new byte[32]));
        sc.AddSingleton<IArmazenamento>(new Sigeo.Infrastructure.Servicos.ArmazenamentoLocal(Path.Combine(Path.GetTempPath(), "sigeo-teste-arquivos")));
        foreach (var t in Classes("Servico").Concat(Classes("Consultas"))) sc.AddScoped(t);
        return sc.BuildServiceProvider();
    }

    // Só "não encontrado" é aceito: o id neutro não existe (First em lista vazia) ou a regra recusa. Erro de SQL ou LINQ falha o teste.
    private static bool Aceito(Exception e) => e is RegraDeNegocioException or ValidacaoException
        || e is InvalidOperationException io && io.Message.StartsWith("Sequence contains no elements", StringComparison.Ordinal);

    [FatoComBanco]
    public async Task Toda_consulta_de_tela_monta_e_roda_no_banco()
    {
        var obra = await Dados.CriarObraAsync(f);
        await using var sp = Montar();
        var falhas = new List<string>();
        var rodadas = 0;
        foreach (var tipo in Classes("Consultas").OrderBy(t => t.Name))
        {
            await using var escopo = sp.CreateAsyncScope();
            object inst;
            try { inst = escopo.ServiceProvider.GetRequiredService(tipo); }
            catch (Exception e) { falhas.Add($"{tipo.Name}: não monta pelo DI ({e.GetBaseException().Message})"); continue; }
            foreach (var m in tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType) && !m.IsGenericMethodDefinition))
            {
                var args = m.GetParameters().Select(p => Argumento(p, obra)).ToArray();
                try
                {
                    await (Task)m.Invoke(inst, args)!;
                    rodadas++;
                }
                catch (Exception e)
                {
                    var real = e is TargetInvocationException t ? t.InnerException ?? e : e;
                    if (!Aceito(real)) falhas.Add($"{tipo.Name}.{m.Name}: {real.GetType().Name}: {real.Message.Split('\n')[0]}");
                    else rodadas++;
                }
            }
        }
        Assert.True(rodadas > 50, $"Só {rodadas} métodos rodaram: a varredura não encontrou as consultas.");
        Assert.Empty(falhas);
    }

    private object? Argumento(ParameterInfo p, Guid obra)
    {
        var t = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;
        var nome = p.Name ?? "";
        if (t == typeof(CancellationToken)) return CancellationToken.None;
        if (t == typeof(Guid)) return nome.Contains("obra", StringComparison.OrdinalIgnoreCase) ? obra : Guid.Empty;
        if (p.HasDefaultValue && p.DefaultValue is not null) return p.DefaultValue;
        if (p.ParameterType != t) return null; // anulável sem padrão: vazio
        if (t == typeof(int)) return 100;
        if (t == typeof(long)) return 100L;
        if (t == typeof(decimal)) return 0m;
        if (t == typeof(bool)) return false;
        if (t == typeof(string)) return "";
        if (t == typeof(DateOnly)) return DateOnly.FromDateTime(DateTime.Today);
        if (t == typeof(DateTime)) return DateTime.Today;
        if (t == typeof(DateTimeOffset)) return DateTimeOffset.Now;
        if (t.IsEnum) return Enum.GetValues(t).GetValue(0);
        if (t.IsArray) return Array.CreateInstance(t.GetElementType()!, 0);
        if (t.IsGenericType && t.GetGenericTypeDefinition() is var g
            && (g == typeof(IReadOnlyCollection<>) || g == typeof(IEnumerable<>) || g == typeof(IReadOnlyList<>) || g == typeof(List<>) || g == typeof(ICollection<>)))
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(t.GetGenericArguments()[0]));
        if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Dictionary<,>) || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
            return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(t.GetGenericArguments()));
        if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(HashSet<>) || t.GetGenericTypeDefinition() == typeof(ISet<>) || t.GetGenericTypeDefinition() == typeof(IReadOnlySet<>)))
            return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(t.GetGenericArguments()[0]));
        if (t.IsValueType) return Activator.CreateInstance(t);
        return null;
    }
}
