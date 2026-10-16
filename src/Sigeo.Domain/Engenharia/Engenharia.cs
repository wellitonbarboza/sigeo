using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

/// <summary>
/// SPE (Sociedade de Propósito Específico) — 1 condomínio (quadra) = 1 SPE = 1 CNPJ. Todo custo, pedido e NF carrega a SPE
/// (segregação de custo por patrimônio; base do rateio e do extrato mensal). As torres do condomínio pertencem à SPE.
/// </summary>
public class Spe : EntidadeAuditavel, IHistoricoTemporal, IPertenceObra
{
    public Guid ObraId { get; set; }
    /// <summary>Quadra/condomínio da SPE (1 para 1).</summary>
    public Guid? QuadraId { get; set; }
    /// <summary>Sigla curta (ex.: IRE).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? EnderecoFaturamento { get; set; }
    public string? EmailFiscal { get; set; }
    /// <summary>Patrimônio de afetação / RET: Sim, Não ou Indefinido (a confirmar com a contabilidade).</summary>
    public string PatrimonioAfetacao { get; set; } = "Indefinido";
    /// <summary>Engenheiro responsável pela SPE: aprovador de 1º nível (alçada "engenheiro responsável") dos documentos da SPE.</summary>
    public Guid? EngenheiroId { get; set; }
    public bool Ativa { get; set; } = true;

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;
}

/// <summary>
/// Quadra / condomínio = SPE. O tipo é configurável (lista "Tipo de agrupamento"). Recebe as torres e as áreas comuns do condomínio.
/// </summary>
public class Quadra : EntidadeAuditavel, IHistoricoTemporal, IPertenceObra
{
    public Guid ObraId { get; set; }
    /// <summary>Sigla usada nos códigos de local (ex.: QC).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Condomínio";
    /// <summary>Nome do condomínio (quando diferente do nome da quadra).</summary>
    public string? NomeCondominio { get; set; }
    public string? CnpjCondominio { get; set; }
    public string? Endereco { get; set; }
    public string? Matricula { get; set; }
    public decimal? AreaTerreno { get; set; }
    public string? Observacoes { get; set; }
    public int Ordem { get; set; }
    public bool Ativa { get; set; } = true;
    /// <summary>Implantação (tipologia com as áreas comuns do condomínio).</summary>
    public Guid? ImplantacaoId { get; set; }
    /// <summary>Local raiz criado pelo gerador (Condomínio › Torre › Pavimento › Apartamento › Cômodo).</summary>
    public Guid? LocalId { get; set; }
}

/// <summary>
/// Tipologia: de APARTAMENTO (cômodos com medidas), de TORRE (pavimentos, mix de apartamentos e áreas comuns da torre/pavimento)
/// ou IMPLANTAÇÃO do condomínio (áreas comuns do condomínio). Os serviços do orçamento são marcados na tipologia.
/// </summary>
public class Tipologia : EntidadeAuditavel, IPertenceObra
{
    public Guid ObraId { get; set; }
    public TipoTipologia Tipo { get; set; } = TipoTipologia.Apartamento;
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Dormitorios { get; set; }
    public int Suites { get; set; }
    public decimal AreaPrivativa { get; set; }
    /// <summary>Área equivalente (NBR 12721) — opcional, para critérios de rateio.</summary>
    public decimal? AreaEquivalente { get; set; }
    /// <summary>Torre: número de pavimentos.</summary>
    public int Pavimentos { get; set; }
    /// <summary>Torre: o 1º pavimento se chama "Térreo".</summary>
    public bool PrimeiroTerreo { get; set; } = true;
    /// <summary>Torre: numeração dos apartamentos pelo andar (térreo 01, 02…; 1º pavimento 101…). Desligado: pela posição do pavimento (101 no 1º da lista).</summary>
    public bool NumeracaoPorAndar { get; set; }
    /// <summary>Pé-direito padrão dos ambientes (m) — usado quando o ambiente não informa o seu.</summary>
    public decimal? PeDireitoPadrao { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<TipologiaAmbiente> Ambientes { get; set; } = [];
    public ICollection<TipologiaMix> Mix { get; set; } = [];

    /// <summary>Soma das áreas de piso dos cômodos (para conferir a área privativa).</summary>
    public decimal SomaAreaComodos => Ambientes.Where(a => a.Escopo == EscopoAmbiente.Comodo).Sum(a => a.AreaPiso);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Codigo)) e.Add("Informe o código.");
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome.");
        if (Tipo == TipoTipologia.Apartamento)
        {
            if (AreaPrivativa <= 0) e.Add("Área privativa deve ser maior que zero.");
            if (Dormitorios < 0 || Suites < 0 || Suites > Dormitorios) e.Add("Suítes não podem passar do número de dormitórios.");
            if (Ambientes.Any(a => a.Escopo != EscopoAmbiente.Comodo)) e.Add("Apartamento só tem cômodos (áreas comuns ficam na torre ou na implantação).");
        }
        if (Tipo == TipoTipologia.Torre)
        {
            if (Pavimentos is < 1 or > 60) e.Add("Torre: pavimentos entre 1 e 60.");
            if (Mix.Sum(m => m.Quantidade) <= 0) e.Add("Torre: informe o mix de apartamentos por pavimento.");
            foreach (var m in Mix)
            {
                if (m.Quantidade is < 1 or > 40) e.Add("Mix: quantidade por pavimento entre 1 e 40.");
                if (m.PavimentoDe < 1 || m.PavimentoAte < m.PavimentoDe || m.PavimentoAte > Pavimentos) e.Add($"Mix: faixa de pavimentos {m.PavimentoDe}–{m.PavimentoAte} inválida.");
                if (m.ApartamentoTipologiaId == Guid.Empty) e.Add("Mix: escolha a tipologia de apartamento.");
            }
            if (Ambientes.Any(a => a.Escopo is EscopoAmbiente.Comodo or EscopoAmbiente.AreaComumCondominio)) e.Add("Torre: use só áreas comuns do pavimento ou da torre.");
        }
        if (Tipo == TipoTipologia.Implantacao && Ambientes.Any(a => a.Escopo != EscopoAmbiente.AreaComumCondominio))
            e.Add("Implantação: use só áreas comuns do condomínio.");
        foreach (var a in Ambientes)
        {
            if (string.IsNullOrWhiteSpace(a.Nome)) e.Add("Ambiente sem nome.");
            if (a.AreaPiso < 0 || a.Perimetro < 0 || a.AreaVaos < 0 || a.PeDireito < 0) e.Add($"{a.Nome}: medidas não podem ser negativas.");
            if (a.Perimetro > 0 && (a.PeDireito ?? PeDireitoPadrao ?? 0) <= 0) e.Add($"{a.Nome}: informe o pé-direito (do ambiente ou o padrão da tipologia) para calcular a área de parede.");
        }
        foreach (var g in Ambientes.GroupBy(a => (a.Sigla ?? "").ToUpperInvariant()).Where(g => g.Count() > 1))
            e.Add($"Sigla de ambiente repetida: {g.Key}.");
        return e;
    }

    /// <summary>Preenche as siglas que faltam (SAL, DOR1, DOR2, BWC…), únicas na tipologia.</summary>
    public void CompletarSiglas()
    {
        var usadas = Ambientes.Where(a => !string.IsNullOrWhiteSpace(a.Sigla)).Select(a => a.Sigla!.ToUpperInvariant()).ToHashSet();
        foreach (var a in Ambientes.OrderBy(a => a.Ordem))
        {
            if (!string.IsNullOrWhiteSpace(a.Sigla)) { a.Sigla = a.Sigla.Trim().ToUpperInvariant(); continue; }
            a.Sigla = GeradorEmpreendimento.SugerirSigla(a.Nome, usadas);
            usadas.Add(a.Sigla);
        }
    }
}

/// <summary>Ambiente-modelo da tipologia: cômodo do apartamento ou área comum (pavimento, torre, condomínio), com as medidas.</summary>
public class TipologiaAmbiente : Entidade
{
    public Guid TipologiaId { get; set; }
    public int Ordem { get; set; }
    public EscopoAmbiente Escopo { get; set; } = EscopoAmbiente.Comodo;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Sigla usada no código do local (ex.: SAL, DOR1, BWC).</summary>
    public string? Sigla { get; set; }
    /// <summary>Categoria da lista padrão de cômodos (Sala, Dormitório, BWC, Cozinha…).</summary>
    public string? Categoria { get; set; }
    public decimal AreaPiso { get; set; }
    public decimal Perimetro { get; set; }
    /// <summary>Pé-direito (m); vazio = padrão da tipologia.</summary>
    public decimal? PeDireito { get; set; }
    /// <summary>Área de vãos (portas e janelas) descontada da parede.</summary>
    public decimal AreaVaos { get; set; }
    /// <summary>Área comum do pavimento: só nos pavimentos desta faixa (vazio = todos).</summary>
    public int? PavimentoDe { get; set; }
    public int? PavimentoAte { get; set; }

    public decimal PeDireitoEfetivo(decimal? padrao) => PeDireito ?? padrao ?? 0;
    public decimal AreaParede(decimal? padrao) => Math.Max(0, Math.Round(Perimetro * PeDireitoEfetivo(padrao) - AreaVaos, 4));
    public bool NoPavimento(int p) => (PavimentoDe is null || p >= PavimentoDe) && (PavimentoAte is null || p <= PavimentoAte);
}

/// <summary>Mix de apartamentos da torre: na faixa de pavimentos, N apartamentos da tipologia (na ordem).</summary>
public class TipologiaMix : Entidade
{
    public Guid TipologiaId { get; set; }
    public int Ordem { get; set; }
    public int PavimentoDe { get; set; } = 1;
    public int PavimentoAte { get; set; } = 1;
    public Guid ApartamentoTipologiaId { get; set; }
    public int Quantidade { get; set; } = 1;
}

/// <summary>Uma torre a gerar no condomínio: nome e tipologia de torre (com mix e áreas comuns carregados).</summary>
public sealed record TorreGerar(string Nome, Tipologia Tipologia);

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
