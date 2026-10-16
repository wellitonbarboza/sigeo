namespace Sigeo.Web.Servicos;

/// <summary>
/// O sistema se chamou QualyHub e depois Groundkey. Sites e cofres já configurados usam chaves "QualyHub:*" / "Groundkey:*"
/// e as conexões "QualyHub" / "Groundkey"; aqui elas são copiadas para "Sigeo:*" / "ConnectionStrings:Sigeo" quando a chave
/// nova não existe. A chave nova sempre vence; entre as antigas, Groundkey (mais recente) vence QualyHub.
/// Pode ser removido quando o Azure tiver só as chaves novas.
/// </summary>
public static class ConfiguracaoLegada
{
    private static readonly string[] Antigos = ["Groundkey", "QualyHub"]; // ordem de preferência
    private const string Novo = "Sigeo";

    public static IReadOnlyDictionary<string, string?> Mapear(IConfiguration cfg)
    {
        var novos = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var itens = cfg.AsEnumerable().Where(kv => kv.Value is not null).ToList();
        foreach (var antigo in Antigos)
            foreach (var (chave, valor) in itens)
            {
                string? destino = null;
                if (chave.StartsWith(antigo + ":", StringComparison.OrdinalIgnoreCase)) destino = Novo + chave[antigo.Length..];
                else if (chave.Equals("ConnectionStrings:" + antigo, StringComparison.OrdinalIgnoreCase)) destino = "ConnectionStrings:" + Novo;
                if (destino is not null && string.IsNullOrEmpty(cfg[destino]) && !novos.ContainsKey(destino)) novos[destino] = valor;
            }
        return novos;
    }

    public static void Aplicar(IConfigurationManager cfg)
    {
        var novos = Mapear(cfg);
        if (novos.Count > 0) cfg.AddInMemoryCollection(novos);
    }
}
