using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Importacao;

/// <summary>
/// Regras puras do de-para de importação: limpeza de nome, similaridade de produto, fase da EAP de custo, local e categoria.
/// Só sugerem — a decisão fica gravada no <see cref="VinculoOrigem"/> pela tela.
/// </summary>
public static partial class SugestoesImportacao
{
    /// <summary>Texto normalizado para chave/comparação: sem acento, maiúsculo, só letras/números, espaços simples.</summary>
    public static string Chave(string? texto)
    {
        var s = new string((texto ?? "").Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
        s = NaoAlfanum().Replace(s, " ");
        return Espacos().Replace(s, " ").Trim();
    }

    /// <summary>
    /// Nome de insumo limpo: tira prefixos de marcação ("# ", "* ") e o "local [fase] - " que o ERP cola no nome
    /// ("1° PAVIMENTO [Instalações…] - Locação de mini escavadeira" → "Locação de mini escavadeira").
    /// </summary>
    public static string LimparNome(string? nome)
    {
        var s = (nome ?? "").Trim();
        var m = NomeColado().Match(s);
        if (m.Success) s = m.Groups[1].Value.Trim();
        s = s.TrimStart('#', '*', '-', ' ', '.');
        s = Espacos().Replace(s, " ").Trim();
        return s.Length > 300 ? s[..300] : s;
    }

    private static readonly HashSet<string> Vazias = ["DE", "DA", "DO", "E", "P", "PARA", "COM", "EM", "A", "O", "C", "X", "MM", "CM", "M"];

    private static HashSet<string> Tokens(string texto) =>
        NumeroUnidade().Replace(Chave(texto), "$1$2").Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !Vazias.Contains(t)).ToHashSet();

    /// <summary>Similaridade 0–1 entre dois nomes de produto (tokens em comum, números pesam: bitola/medida diferente = outro produto).</summary>
    public static decimal Similaridade(string a, string b)
    {
        var ta = Tokens(a); var tb = Tokens(b);
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var numA = ta.Where(EhNumero).ToHashSet(); var numB = tb.Where(EhNumero).ToHashSet();
        if (numA.Count > 0 && numB.Count > 0 && !numA.SetEquals(numB)) return 0.3m * Jaccard(ta, tb);
        return Math.Round(Jaccard(ta, tb), 3);
    }

    private static bool EhNumero(string t) => t.Any(char.IsDigit);
    private static decimal Jaccard(HashSet<string> a, HashSet<string> b) => (decimal)a.Intersect(b).Count() / a.Union(b).Count();

    public sealed record Candidato(Guid Id, string Nome, string Unidade);
    public sealed record Sugestao(Guid Id, string Nome, string Unidade, decimal Nota);

    /// <summary>Melhores produtos do catálogo para o nome de origem (mesma unidade soma pontos).</summary>
    public static IReadOnlyList<Sugestao> Produtos(string nomeOrigem, string unidadeOrigem, IEnumerable<Candidato> catalogo, int quantos = 3) =>
        new IndiceProdutos(catalogo).Sugerir(nomeOrigem, unidadeOrigem, quantos);

    /// <summary>
    /// Catálogo pré-processado para sugerir em lote (centenas de insumos × milhares de produtos): tokens calculados uma vez e
    /// índice invertido por palavra — só compara com produtos que têm alguma palavra em comum.
    /// </summary>
    public sealed class IndiceProdutos
    {
        private readonly List<(Candidato C, string Chave, HashSet<string> Tokens, HashSet<string> Nums)> _itens = [];
        private readonly Dictionary<string, List<int>> _porToken = [];
        private readonly Dictionary<string, int> _porChave = [];

        public IndiceProdutos(IEnumerable<Candidato> catalogo)
        {
            foreach (var c in catalogo)
            {
                var t = Tokens(c.Nome);
                var i = _itens.Count;
                _itens.Add((c, Chave(c.Nome), t, t.Where(EhNumero).ToHashSet()));
                _porChave.TryAdd(_itens[i].Chave, i);
                foreach (var tok in t.Where(x => !EhNumero(x)))
                {
                    if (!_porToken.TryGetValue(tok, out var l)) _porToken[tok] = l = [];
                    l.Add(i);
                }
            }
        }

        public IReadOnlyList<Sugestao> Sugerir(string nomeOrigem, string unidadeOrigem, int quantos = 3)
        {
            var un = Cadastros.UnidadesPadrao.Normalizar(unidadeOrigem);
            var chave = Chave(nomeOrigem);
            var ta = Tokens(nomeOrigem);
            if (ta.Count == 0) return [];
            var numA = ta.Where(EhNumero).ToHashSet();
            var candidatos = new HashSet<int>(ta.Where(t => !EhNumero(t)).SelectMany(t => _porToken.GetValueOrDefault(t) ?? []));
            if (_porChave.TryGetValue(chave, out var exato)) candidatos.Add(exato);
            return candidatos
                .Select(i =>
                {
                    var (c, ch, tb, numB) = _itens[i];
                    decimal nota;
                    if (ch == chave) nota = 1m;
                    else
                    {
                        var j = Jaccard(ta, tb);
                        nota = numA.Count > 0 && numB.Count > 0 && !numA.SetEquals(numB) ? 0.3m * j : Math.Round(j, 3);
                    }
                    if (nota > 0 && c.Unidade != un) nota *= 0.85m;
                    return new Sugestao(c.Id, c.Nome, c.Unidade, Math.Round(nota, 3));
                })
                .Where(s => s.Nota >= 0.4m).OrderByDescending(s => s.Nota).ThenBy(s => s.Nome).Take(quantos).ToList();
        }
    }

    /// <summary>Nota mínima para a sugestão virar vínculo automático (sem o usuário escolher).</summary>
    public const decimal NotaVinculoAutomatico = 0.9m;

    /// <summary>Fases indiretas da obra (complementam os grupos de serviço da EAP — custos que não são de um serviço/local).</summary>
    public static readonly string[] FasesIndiretas =
    [
        "Projetos, ARTs e licenças", "Canteiro e instalações provisórias", "Equipe técnica e administrativa", "Despesas administrativas da obra",
        "Segurança e medicina do trabalho", "Equipamentos e ferramentas", "Controle tecnológico", "Seguros e vigilância",
        "Prevenção e combate a incêndio", Engenharia.CustoRealizado.FaseAApropriar
    ];

    // Palavra-chave da fase de origem → grupo de serviço (ordem importa: a primeira que casar vale).
    private static readonly (string[] Chaves, string Fase)[] RegrasFase =
    [
        (["PROJETO", "ART", "RRT", "ALVARA", "LICENCA"], "Projetos, ARTs e licenças"),
        (["INSTALACOES PROVISORIAS", "SERVICOS INICIAIS", "MOBILIZACAO", "CANTEIRO", "TAPUME"], "Canteiro e instalações provisórias"),
        (["SALARIO", "EQUIPE TECNICA", "ENCARGOS DA EQUIPE"], "Equipe técnica e administrativa"),
        (["DESPESAS ADMINISTRATIVAS", "ESCRITORIO", "COMBUSTIVEL"], "Despesas administrativas da obra"),
        (["MEDICINA", "SEGURANCA DO TRABALHO", "PROTECAO COLETIVA", "EPI", "EPC"], "Segurança e medicina do trabalho"),
        (["EQUIPAMENTOS E FERRAMENTAS", "LOCACOES E AQUISICOES"], "Equipamentos e ferramentas"),
        (["CONTROLE TECNOLOGICO", "LABORATORIO", "ENSAIO"], "Controle tecnológico"),
        (["SEGURO", "VIGILANCIA"], "Seguros e vigilância"),
        (["INCENDIO", "SPRINKLER", "HIDRANTE"], "Prevenção e combate a incêndio"),
        (["MURO DE ARRIMO", "FUNDAC", "ESTACA", "BLOCO DE COROAMENTO", "TRANSICAO", "BALDRAME", "ARRIMO", "TERRAPLENAGEM", "TERRAPLANAGEM"], "Infraestrutura / fundações"),
        (["SUPRAESTRUTURA", "ALVENARIA ESTRUTURAL", "LAJE", "ESCADA", "PRE MOLDAD"], "Estrutura / pré-moldados"),
        (["HIDROSSANIT", "HIDRAULIC", "ESGOTO", "PLUVIAL"], "Instalações hidrossanitárias"),
        (["ELETRIC", "SPDA", "TELEFON", "LOGICA"], "Instalações elétricas"),
        (["IMPERMEABILIZ"], "Impermeabilização"),
        (["CONTRAPISO", "REGULARIZACAO", "PISO", "RODAPE"], "Revestimentos de piso"),
        (["FORRO", "GESSO"], "Forros"),
        (["ESQUADRIA", "JANELA", "PORTA", "VIDRO"], "Esquadrias"),
        (["PINTURA", "TEXTURA"], "Pintura"),
        (["LOUCA", "METAIS SANITARIOS"], "Louças e metais"),
        (["COBERTURA", "TELHADO", "TELHA"], "Cobertura"),
        (["REVESTIMENTO", "REBOCO", "EMBOCO", "AZULEJO", "CERAMICA"], "Revestimentos de parede"),
        (["ALVENARIA", "VEDACAO", "DRYWALL"], "Vedações"),
        (["PORTOES", "FECHAMENTO PERIMETRAL", "MURO", "PAISAGISMO", "URBANIZACAO", "AREA COMUM", "AREAS COMUNS"], "Áreas comuns e urbanização"),
        (["LIMPEZA", "ENTREGA"], "Limpeza e entrega"),
    ];

    /// <summary>Fase (grupo de serviço) sugerida para a fase de origem; só devolve uma fase que exista na lista da construtora.</summary>
    public static string SugerirFase(string? faseOrigem, IReadOnlyCollection<string> fasesDisponiveis)
    {
        var chave = Chave(faseOrigem);
        if (chave.Length == 0 || chave == "-") return Engenharia.CustoRealizado.FaseAApropriar;
        var exata = fasesDisponiveis.FirstOrDefault(f => Chave(f) == chave);
        if (exata is not null) return exata;
        // Vence a palavra-chave que aparece primeiro no texto ("Impermeabilização de … Baldrame" é impermeabilização); empate = ordem das regras.
        var melhor = RegrasFase.Select((r, ordem) => (r.Fase, ordem, Pos: r.Chaves
                .Select(k => Regex.Match(chave, $@"(^|\s){Regex.Escape(k)}")).Where(m => m.Success).Select(m => m.Index).DefaultIfEmpty(int.MaxValue).Min()))
            .Where(x => x.Pos < int.MaxValue && fasesDisponiveis.Contains(x.Fase))
            .OrderBy(x => x.Pos).ThenBy(x => x.ordem).FirstOrDefault();
        return melhor.Fase ?? Engenharia.CustoRealizado.FaseAApropriar;
    }

    public sealed record LocalCandidato(Guid Id, string Nome, string Tipo, int Ordem);

    /// <summary>
    /// Local sugerido para o "serviço" de origem quando ele é um pavimento ("1° PAVIMENTO", "TÉRREO", "COBERTURA").
    /// Nome igual vence; senão casa o número do pavimento. Fase genérica (ex.: "Despesas Administrativas") = sem local (obra toda).
    /// </summary>
    public static Guid? SugerirLocal(string? servicoOrigem, IReadOnlyList<LocalCandidato> locais)
    {
        var chave = Chave(servicoOrigem);
        if (chave.Length == 0) return null;
        var igual = locais.FirstOrDefault(l => Chave(l.Nome) == chave);
        if (igual is not null) return igual.Id;
        var pav = locais.Where(l => l.Tipo.Equals("Pavimento", StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.Ordem).ToList();
        if (chave.Contains("TERREO")) return pav.FirstOrDefault(l => Chave(l.Nome).Contains("TERREO"))?.Id;
        foreach (var palavra in new[] { "COBERTURA", "SUBSOLO", "MEZANINO", "ATICO", "BARRILETE" })
            if (chave.Contains(palavra)) return pav.FirstOrDefault(l => Chave(l.Nome).Contains(palavra))?.Id;
        var num = NumeroPavimento().Match(chave);
        if (!num.Success || !chave.Contains("PAV")) return null;
        var n = num.Groups[1].Value;
        return pav.FirstOrDefault(l => NumeroPavimento().Match(Chave(l.Nome)) is { Success: true } m && m.Groups[1].Value == n && Chave(l.Nome).Contains("PAV"))?.Id;
    }

    /// <summary>Tipo de custo a partir do texto do ERP ("Materiais", "Mão de obra", "Serviço", "Equipamentos", "Fretes"…).</summary>
    public static TipoCusto TipoCustoDe(string? texto)
    {
        var c = Chave(texto);
        if (c.StartsWith("MAO")) return TipoCusto.MaoObra;
        if (c.StartsWith("SERV")) return TipoCusto.Servico;
        if (c.StartsWith("EQUIP") || c.StartsWith("LOCAC")) return TipoCusto.Equipamento;
        if (c.StartsWith("FRETE")) return TipoCusto.Frete;
        if (c.StartsWith("MAT") || c.Length == 0) return TipoCusto.Material;
        return TipoCusto.Outros;
    }

    // Categoria do ERP → grupo do catálogo (o que não casar fica "A Cadastrar", para o cadastro classificar).
    private static readonly (string Chave, string Grupo)[] RegrasCategoria =
    [
        ("HIDRAUL", "Hidráulica"), ("ELETRIC", "Material Elétrico"), ("EPI", "EPI"), ("FERRAMENT", "Ferramentas"), ("LIMPEZA", "Material de Limpeza"),
        ("ESCRITORIO", "Material de Escritório"), ("PINTURA", "Tintas, Texturas e Massas"), ("MADEIRA", "Chapas Compensadas e Madeiras Exóticas"),
        ("FERRAGEM", "Ferragens"), ("ACO", "Aço e Armaduras"), ("UNIFORME", "Uniformes"), ("LUBRIFIC", "Lubrificantes"), ("SOLDA", "Solda"),
    ];

    /// <summary>Grupo do catálogo para a categoria do ERP, só se o grupo existir na construtora; senão "A Cadastrar".</summary>
    public static string GrupoDaCategoria(string? categoria, IReadOnlyCollection<string> gruposExistentes)
    {
        var c = Chave(categoria);
        var exato = gruposExistentes.FirstOrDefault(g => Chave(g) == c);
        if (exato is not null && c.Length > 0) return exato;
        foreach (var (k, g) in RegrasCategoria)
            if (c.Contains(k) && gruposExistentes.Contains(g)) return g;
        return "A Cadastrar";
    }

    [GeneratedRegex(@"(\d)\s+(MM|CM|M|KG|G|L|ML|M2|M3|POL)\b")] private static partial Regex NumeroUnidade();
    [GeneratedRegex(@"[^A-Z0-9 ]")] private static partial Regex NaoAlfanum();
    [GeneratedRegex(@"\s+")] private static partial Regex Espacos();
    [GeneratedRegex(@"^.{0,80}\[[^\]]+\]\s*-\s*(.+)$")] private static partial Regex NomeColado();
    [GeneratedRegex(@"(?:^|\s)(\d{1,2})(?:\s|O\b|$)")] private static partial Regex NumeroPavimento();
}
