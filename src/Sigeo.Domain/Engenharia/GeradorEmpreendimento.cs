using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// Gera Condomínio (SPE) › Torre › Pavimento › Apartamento › Cômodo + áreas comuns (condomínio, torre, pavimento) a partir das tipologias,
/// e calcula a fração ideal (área privativa ÷ soma do condomínio). Código: QC-T01-P02-201-SAL.
/// </summary>
public static class GeradorEmpreendimento
{
    public const string TipoCondominio = "Condomínio", TipoTorre = "Torre", TipoPavimento = "Pavimento", TipoApartamento = "Apartamento",
        TipoComodo = "Cômodo", TipoAreaComum = "Área comum";

    /// <summary>Níveis criados pelo gerador (não podem ser criados à mão).</summary>
    public static readonly string[] Gerados = [TipoCondominio, TipoTorre, TipoPavimento, TipoApartamento, TipoComodo];

    /// <param name="obraPropria">Obra própria (dono do custo = construtora): gera sem SPE; os locais ficam sem SPE até ela ser constituída.</param>
    public static IReadOnlyList<string> Validar(Quadra quadra, Guid? speId, IReadOnlyList<TorreGerar> torres, IReadOnlyDictionary<Guid, Tipologia> aptos, bool obraPropria = false)
    {
        var e = new List<string>();
        if ((speId is null || speId == Guid.Empty) && !obraPropria) e.Add($"{quadra.Nome}: vincule a SPE do condomínio antes de gerar (ou marque a obra como própria, com o custo na construtora).");
        if (torres.Count == 0) e.Add("Informe ao menos uma torre.");
        foreach (var t in torres)
        {
            if (string.IsNullOrWhiteSpace(t.Nome)) e.Add("Torre sem nome.");
            if (t.Tipologia.Tipo != TipoTipologia.Torre) { e.Add($"{t.Nome}: escolha uma tipologia de torre."); continue; }
            e.AddRange(t.Tipologia.Validar().Select(x => $"{t.Nome}: {x}"));
            foreach (var m in t.Tipologia.Mix)
                if (!aptos.TryGetValue(m.ApartamentoTipologiaId, out var ap) || ap.Tipo != TipoTipologia.Apartamento) e.Add($"{t.Nome}: tipologia de apartamento do mix não encontrada.");
                else if (ap.AreaPrivativa <= 0) e.Add($"{t.Nome}: tipologia {ap.Codigo} sem área privativa.");
        }
        if (torres.GroupBy(t => t.Nome.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) e.Add("Nomes de torre repetidos.");
        return e;
    }

    public static string NomePavimento(int p, bool primeiroTerreo) => primeiroTerreo ? (p == 1 ? "Térreo" : $"{p - 1}º pavimento") : $"{p}º pavimento";

    /// <summary>
    /// Monta a árvore. Apartamento: número = pavimento×100 + sequência (101, 102…). Torres numeradas a partir de <paramref name="ordemTorreInicial"/>+1.
    /// </summary>
    /// <param name="condominioExistente">Local do condomínio já gerado (acrescenta torres); nulo cria o condomínio e as áreas comuns dele.</param>
    public static List<Local> Gerar(Guid obraId, Quadra quadra, Guid? speId, Tipologia? implantacao, IReadOnlyList<TorreGerar> torres,
        IReadOnlyDictionary<Guid, Tipologia> aptos, Local? condominioExistente = null, int ordemTorreInicial = 0, string prefixoUnidade = "Apto")
    {
        prefixoUnidade = string.IsNullOrWhiteSpace(prefixoUnidade) ? "Apto" : prefixoUnidade.Trim();
        var lista = new List<Local>();
        var cond = condominioExistente;
        if (cond is null)
        {
            cond = new Local { ObraId = obraId, Tipo = TipoCondominio, Nome = quadra.Nome.Trim(), Codigo = quadra.Codigo, Ordem = quadra.Ordem, SpeId = speId, TipologiaId = implantacao?.Id };
            lista.Add(cond);
            if (implantacao is not null)
                foreach (var a in implantacao.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumCondominio).OrderBy(a => a.Ordem))
                    lista.Add(Ambiente(obraId, cond, a, implantacao.PeDireitoPadrao, TipoAreaComum));
        }
        var ordem = ordemTorreInicial;
        foreach (var t in torres)
        {
            ordem++;
            var tt = t.Tipologia;
            var torre = new Local { ObraId = obraId, Tipo = TipoTorre, Nome = t.Nome.Trim(), Codigo = $"{quadra.Codigo}-T{ordem:00}", Ordem = ordem, LocalPaiId = cond.Id, SpeId = speId, TipologiaId = tt.Id };
            lista.Add(torre);
            foreach (var a in tt.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumTorre).OrderBy(a => a.Ordem))
                lista.Add(Ambiente(obraId, torre, a, tt.PeDireitoPadrao, TipoAreaComum));
            for (var p = 1; p <= tt.Pavimentos; p++)
            {
                var pav = new Local { ObraId = obraId, Tipo = TipoPavimento, Nome = NomePavimento(p, tt.PrimeiroTerreo), Codigo = $"{torre.Codigo}-P{p:00}", Ordem = p, LocalPaiId = torre.Id, SpeId = speId };
                lista.Add(pav);
                foreach (var a in tt.Ambientes.Where(a => a.Escopo == EscopoAmbiente.AreaComumPavimento && a.NoPavimento(p)).OrderBy(a => a.Ordem))
                    lista.Add(Ambiente(obraId, pav, a, tt.PeDireitoPadrao, TipoAreaComum));
                var seq = 0;
                foreach (var m in tt.Mix.Where(m => p >= m.PavimentoDe && p <= m.PavimentoAte).OrderBy(m => m.Ordem))
                {
                    var ap = aptos[m.ApartamentoTipologiaId];
                    for (var i = 0; i < m.Quantidade; i++)
                    {
                        seq++;
                        var numero = NumeroApartamento(p, seq, tt.PrimeiroTerreo && tt.NumeracaoPorAndar);
                        var u = new Local
                        {
                            ObraId = obraId, Tipo = TipoApartamento, Nome = $"{prefixoUnidade} {numero}", Codigo = $"{pav.Codigo}-{numero}", Ordem = seq, LocalPaiId = pav.Id,
                            SpeId = speId, TipologiaId = ap.Id, AreaPrivativa = ap.AreaPrivativa, Quantidade = ap.AreaPrivativa, Unidade = "m²"
                        };
                        lista.Add(u);
                        foreach (var a in ap.Ambientes.Where(a => a.Escopo == EscopoAmbiente.Comodo).OrderBy(a => a.Ordem))
                            lista.Add(Ambiente(obraId, u, a, ap.PeDireitoPadrao, TipoComodo));
                    }
                }
            }
        }
        return lista;
    }

    /// <summary>Número do apartamento: pavimento×100 + sequência (101…); por andar, o térreo fica 01, 02… e o 1º pavimento 101.</summary>
    public static string NumeroApartamento(int pavimento, int seq, bool porAndar)
    {
        var andar = porAndar ? pavimento - 1 : pavimento;
        return andar == 0 ? seq.ToString("00") : (andar * 100 + seq).ToString();
    }

    /// <summary>Cria o local de um ambiente-modelo dentro do pai, copiando as medidas.</summary>
    public static Local Ambiente(Guid obraId, Local pai, TipologiaAmbiente a, decimal? peDireitoPadrao, string tipo)
    {
        var l = new Local
        {
            ObraId = obraId, Tipo = tipo, Nome = a.Nome, Codigo = $"{pai.Codigo}-{a.Sigla}", Ordem = a.Ordem, LocalPaiId = pai.Id, SpeId = pai.SpeId,
            Categoria = a.Categoria, AmbienteModeloId = a.Id
        };
        CopiarMedidas(l, a, peDireitoPadrao);
        return l;
    }

    public static void CopiarMedidas(Local l, TipologiaAmbiente a, decimal? peDireitoPadrao)
    {
        l.AreaPiso = a.AreaPiso; l.Perimetro = a.Perimetro; l.PeDireito = a.PeDireitoEfetivo(peDireitoPadrao); l.AreaParede = a.AreaParede(peDireitoPadrao);
        l.Quantidade = a.AreaPiso; l.Unidade = "m²"; l.Categoria = a.Categoria;
    }

    /// <summary>Fração ideal = área privativa ÷ soma dos apartamentos (6 casas; resíduo no último, soma exata 1).</summary>
    public static void DistribuirFracao(IList<Local> unidades)
    {
        var total = unidades.Sum(u => u.AreaPrivativa ?? 0);
        if (total <= 0 || unidades.Count == 0) return;
        foreach (var u in unidades) u.FracaoIdeal = Math.Round((u.AreaPrivativa ?? 0) / total, 6);
        unidades[^1].FracaoIdeal += 1m - unidades.Sum(u => u.FracaoIdeal ?? 0);
    }

    /// <summary>Sobe a árvore até o nível pedido (ex.: condomínio do apartamento).</summary>
    public static Local? Ancestral(Local l, string tipo, IReadOnlyDictionary<Guid, Local> porId)
    {
        Local? x = l;
        while (x is not null && x.Tipo != tipo) x = x.LocalPaiId is { } pid && porId.TryGetValue(pid, out var pai) ? pai : null;
        return x;
    }

    /// <summary>Conferência: soma da fração ideal dos apartamentos de cada condomínio deve ser 100% (±0,0001%).</summary>
    public static IReadOnlyList<string> ConferirFracao(IEnumerable<Local> locais)
    {
        var e = new List<string>();
        var porId = locais.ToDictionary(l => l.Id);
        foreach (var g in porId.Values.Where(l => l.Tipo == TipoApartamento).GroupBy(l => Ancestral(l, TipoCondominio, porId)))
        {
            if (g.Key is null) continue;
            var soma = g.Sum(u => u.FracaoIdeal ?? 0);
            if (Math.Abs(soma - 1m) > 0.000001m) e.Add($"{g.Key.Nome}: soma da fração ideal = {soma * 100:0.0000}% (deve ser 100%).");
        }
        return e;
    }

    /// <summary>Sigla curta para ambiente: 3 letras + número do nome (Dormitório 02 → DOR2), única na tipologia.</summary>
    public static string SugerirSigla(string nome, ICollection<string> usadas)
    {
        var limpo = new string((nome ?? "").Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
        var palavras = limpo.Split([' ', '-', '.', '/', '_'], StringSplitOptions.RemoveEmptyEntries).ToList();
        var letras = palavras.Where(w => w.Any(char.IsLetter) && w is not ("DE" or "DA" or "DO" or "E")).ToList();
        var num = palavras.LastOrDefault(w => w.All(char.IsDigit));
        var baseS = letras.Count switch
        {
            0 => "AMB",
            1 => new string(letras[0].Where(char.IsLetterOrDigit).Take(3).ToArray()).PadRight(3, 'X'),
            _ => (letras[0][..1] + new string(letras[1].Where(char.IsLetterOrDigit).Take(2).ToArray())).PadRight(3, 'X')
        };
        if (letras.Count == 1 && letras[0] is "BWC" or "WC") baseS = letras[0];
        if (letras.Count >= 2 && letras[0] == "AREA" && letras[1] == "SERVICO") baseS = "ASV";
        if (letras.Count >= 2 && letras[0] == "SALA" && letras[1] == "ESTAR") baseS = "SAL";
        if (letras.Count >= 2 && letras[0] == "SALA" && letras[1] == "JANTAR") baseS = "SJA";
        var c = num is null ? baseS : baseS + int.Parse(num, CultureInfo.InvariantCulture);
        if (!usadas.Contains(c)) return c;
        for (var n = 2; n < 100; n++) if (!usadas.Contains(baseS + n)) return baseS + n;
        throw new RegraDeNegocioException("Não foi possível sugerir a sigla do ambiente.");
    }
}
