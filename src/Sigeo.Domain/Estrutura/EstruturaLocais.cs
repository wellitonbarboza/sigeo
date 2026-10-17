using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Como a construtora chama os níveis de local. O gerador continua Condomínio (dono do CNPJ/SPE) › Torre › Pavimento › Unidade › Cômodo;
/// cada construtora dá o nome que usa (ex.: loteamento = Loteamento › Quadra › Nível › Casa › Cômodo, prefixo "Casa").
/// </summary>
public sealed record EstruturaLocais(string Modelo, string Condominio, string Torre, string Pavimento, string Unidade, string Comodo, string PrefixoUnidade)
{
    public static readonly EstruturaLocais Condominios = new("Condominio", "Condomínio", "Torre", "Pavimento", "Apartamento", "Cômodo", "Apto");
    public static readonly EstruturaLocais Edificio = new("Edificio", "Empreendimento", "Torre", "Pavimento", "Apartamento", "Cômodo", "Apto");
    public static readonly EstruturaLocais Loteamento = new("Loteamento", "Loteamento", "Quadra", "Nível", "Casa", "Cômodo", "Casa");
    public static readonly IReadOnlyList<(EstruturaLocais Modelo, string Titulo, string Uso)> Modelos =
    [
        (Condominios, "Condomínios com SPE (tipo Ecoparque)", "Vários condomínios, cada um com CNPJ próprio (SPE) e torres geradas por tipologia."),
        (Edificio, "Edifício", "Um empreendimento (uma SPE) com uma ou mais torres."),
        (Loteamento, "Loteamento / casas", "Quadras com casas ou lotes; a unidade é a casa."),
    ];

    public string[] Niveis => [Condominio, Torre, Pavimento, Unidade, Comodo];

    public static EstruturaLocais De(string? modelo, string? nomes)
    {
        var baseM = Modelos.Select(m => m.Modelo).FirstOrDefault(m => m.Modelo == modelo) ?? Condominios;
        var p = (nomes ?? "").Split(';');
        string N(int i, string padrao) => p.Length > i && !string.IsNullOrWhiteSpace(p[i]) ? p[i].Trim() : padrao;
        return new(modelo is { Length: > 0 } ? modelo : baseM.Modelo, N(0, baseM.Condominio), N(1, baseM.Torre), N(2, baseM.Pavimento), N(3, baseM.Unidade), N(4, baseM.Comodo), N(5, baseM.PrefixoUnidade));
    }

    public string Serializar() => string.Join(";", Condominio, Torre, Pavimento, Unidade, Comodo, PrefixoUnidade);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        foreach (var (n, nome) in new[] { (Condominio, "SPE"), (Torre, "bloco/torre"), (Pavimento, "pavimento"), (Unidade, "unidade"), (Comodo, "cômodo") })
            if (string.IsNullOrWhiteSpace(n) || n.Length > 40 || n.Contains(';')) e.Add($"Nome do nível {nome}: de 1 a 40 caracteres, sem \";\".");
        if (string.IsNullOrWhiteSpace(PrefixoUnidade) || PrefixoUnidade.Length > 12 || PrefixoUnidade.Contains(';')) e.Add("Prefixo da unidade: de 1 a 12 caracteres.");
        return e;
    }

    /// <summary>Nome exibido para o tipo interno do local (Condomínio, Torre, Pavimento, Apartamento, Cômodo).</summary>
    public string Rotulo(string? tipo) => tipo switch
    {
        "Condomínio" => Condominio, "Torre" => Torre, "Pavimento" => Pavimento, "Apartamento" => Unidade, "Cômodo" => Comodo, _ => tipo ?? ""
    };
}
