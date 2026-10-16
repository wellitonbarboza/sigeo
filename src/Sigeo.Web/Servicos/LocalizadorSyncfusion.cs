using System.Collections.Frozen;
using System.Resources;
using System.Text.Json;

namespace Sigeo.Web.Servicos;

/// <summary>
/// Textos do Gantt/grade da Syncfusion em português (chaves do tipo "Gantt_ZoomIn"), com os termos de obra
/// (TI/II/TT/IT, Atividade, Avanço). Chave sem tradução volta nula e o componente usa o texto padrão.
/// </summary>
public sealed class LocalizadorSyncfusion : Syncfusion.Blazor.ISyncfusionStringLocalizer
{
    private static readonly FrozenDictionary<string, string> Textos = Carregar();

    private static FrozenDictionary<string, string> Carregar()
    {
        using var s = typeof(LocalizadorSyncfusion).Assembly.GetManifestResourceStream("syncfusion-pt-BR.json");
        if (s is null) return FrozenDictionary<string, string>.Empty;
        var d = JsonSerializer.Deserialize<Dictionary<string, string>>(s) ?? [];
        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public ResourceManager ResourceManager => null!; // textos vêm do dicionário (GetText)

    public string GetText(string key) => Textos.TryGetValue(key, out var t) ? t : null!; // nulo = texto padrão do componente
}
