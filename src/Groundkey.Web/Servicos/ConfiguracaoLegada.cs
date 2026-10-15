namespace Groundkey.Web.Servicos;

/// <summary>
/// O sistema se chamava QualyHub. Sites e cofres já configurados usam chaves "QualyHub:*" e a conexão "QualyHub";
/// aqui elas são copiadas para "Groundkey:*" / "ConnectionStrings:Groundkey" quando a chave nova não existe.
/// A chave nova sempre vence. Pode ser removido quando o Azure tiver só as chaves novas.
/// </summary>
public static class ConfiguracaoLegada
{
    private const string Antigo = "QualyHub";
    private const string Novo = "Groundkey";

    public static IReadOnlyDictionary<string, string?> Mapear(IConfiguration cfg)
    {
        var novos = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (chave, valor) in cfg.AsEnumerable())
        {
            if (valor is null) continue;
            string? destino = null;
            if (chave.StartsWith(Antigo + ":", StringComparison.OrdinalIgnoreCase)) destino = Novo + chave[Antigo.Length..];
            else if (chave.Equals("ConnectionStrings:" + Antigo, StringComparison.OrdinalIgnoreCase)) destino = "ConnectionStrings:" + Novo;
            if (destino is not null && string.IsNullOrEmpty(cfg[destino])) novos[destino] = valor;
        }
        return novos;
    }

    public static void Aplicar(IConfigurationManager cfg)
    {
        var novos = Mapear(cfg);
        if (novos.Count > 0) cfg.AddInMemoryCollection(novos);
    }
}
