using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Domain.Engenharia;

/// <summary>
/// SPE (Sociedade de Propósito Específico) — 1 prédio = 1 SPE = 1 CNPJ. Todo custo, pedido e NF carrega a SPE
/// (segregação de custo por patrimônio; base do rateio e do extrato mensal).
/// </summary>
public class Spe : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid ObraId { get; set; }
    /// <summary>Quadra/condomínio a que o prédio da SPE pertence.</summary>
    public Guid? QuadraId { get; set; }
    /// <summary>Sigla curta usada nos códigos de local (ex.: IRE).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? EnderecoFaturamento { get; set; }
    public string? EmailFiscal { get; set; }
    /// <summary>Patrimônio de afetação / RET: Sim, Não ou Indefinido (a confirmar com a contabilidade).</summary>
    public string PatrimonioAfetacao { get; set; } = "Indefinido";
    public bool Ativa { get; set; } = true;

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;
}

/// <summary>
/// Quadra / condomínio (agrupamento de prédios/SPEs). O tipo é configurável (lista "Tipo de agrupamento":
/// Quadra, Condomínio, Loteamento…). Base do rateio dos custos comuns entre as SPEs.
/// </summary>
public class Quadra : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid ObraId { get; set; }
    /// <summary>Sigla usada nos códigos de local (ex.: QC).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Quadra";
    /// <summary>Nome do condomínio (quando diferente do nome da quadra).</summary>
    public string? NomeCondominio { get; set; }
    public string? CnpjCondominio { get; set; }
    public string? Endereco { get; set; }
    public string? Matricula { get; set; }
    public decimal? AreaTerreno { get; set; }
    public string? Observacoes { get; set; }
    public int Ordem { get; set; }
    public bool Ativa { get; set; } = true;
    /// <summary>Local raiz criado pelo gerador (Quadra › Prédio › Pavimento › Unidade).</summary>
    public Guid? LocalId { get; set; }
}

/// <summary>Tipologia da unidade (ex.: 2Q 56 m²). O kit de entrega e a lista de peças serão por tipologia.</summary>
public class Tipologia : EntidadeAuditavel
{
    public Guid ObraId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Dormitorios { get; set; }
    public int Suites { get; set; }
    public decimal AreaPrivativa { get; set; }
    /// <summary>Área equivalente (NBR 12721) — opcional, para critérios de rateio.</summary>
    public decimal? AreaEquivalente { get; set; }
    public bool Ativa { get; set; } = true;
}

/// <summary>Um prédio a gerar: SPE dona, nº de pavimentos e mix de unidades por pavimento (pavimentos iguais).</summary>
public sealed record PredioGerar(string Nome, Guid SpeId, string SiglaSpe, int Pavimentos, IReadOnlyList<(Tipologia Tipologia, int Quantidade)> MixPorPavimento);

/// <summary>Gera Quadra › Prédio › Pavimento › Unidade e calcula a fração ideal (proporcional à área privativa, 100% por prédio).</summary>
public static class GeradorEmpreendimento
{
    public const string TipoQuadra = "Quadra", TipoPredio = "Prédio", TipoPavimento = "Pavimento", TipoUnidade = "Unidade";

    public static IReadOnlyList<string> Validar(string quadra, IReadOnlyList<PredioGerar> predios)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(quadra)) e.Add("Informe o nome da quadra.");
        if (predios.Count == 0) e.Add("Informe ao menos um prédio.");
        foreach (var p in predios)
        {
            if (string.IsNullOrWhiteSpace(p.Nome)) e.Add("Prédio sem nome.");
            if (p.SpeId == Guid.Empty) e.Add($"{p.Nome}: selecione a SPE.");
            if (p.Pavimentos is < 1 or > 60) e.Add($"{p.Nome}: pavimentos entre 1 e 60.");
            if (p.MixPorPavimento.Sum(m => m.Quantidade) is < 1 or > 99) e.Add($"{p.Nome}: informe de 1 a 99 unidades por pavimento.");
            if (p.MixPorPavimento.Any(m => m.Tipologia.AreaPrivativa <= 0)) e.Add($"{p.Nome}: tipologia sem área privativa.");
        }
        if (predios.GroupBy(p => p.SpeId).Any(g => g.Count() > 1)) e.Add("Cada SPE só pode ter um prédio (1 prédio = 1 SPE).");
        return e;
    }

    /// <summary>
    /// Monta a árvore. Unidade: número = pavimento×100 + sequência (101, 102…); código = QUADRA-SPE-P01-101.
    /// Fração ideal = área privativa ÷ soma do prédio, 6 casas, resíduo de arredondamento na última unidade (soma exata 1).
    /// </summary>
    /// <param name="quadraExistente">Local da quadra já gerado (acrescenta prédios a ela); nulo cria o local da quadra.</param>
    public static List<Local> Gerar(Guid obraId, string quadra, string siglaQuadra, IReadOnlyList<PredioGerar> predios, Local? quadraExistente = null, int ordemInicial = 0)
    {
        var lista = new List<Local>();
        var q = quadraExistente ?? new Local { ObraId = obraId, Tipo = TipoQuadra, Nome = quadra.Trim(), Codigo = siglaQuadra, Ordem = 1 };
        if (quadraExistente is null) lista.Add(q);
        var ordemPredio = ordemInicial;
        foreach (var p in predios)
        {
            var pr = new Local { ObraId = obraId, Tipo = TipoPredio, Nome = p.Nome.Trim(), Codigo = $"{siglaQuadra}-{p.SiglaSpe}", Ordem = ++ordemPredio, LocalPaiId = q.Id, SpeId = p.SpeId };
            lista.Add(pr);
            var unidades = new List<Local>();
            for (var pav = 1; pav <= p.Pavimentos; pav++)
            {
                var pv = new Local { ObraId = obraId, Tipo = TipoPavimento, Nome = $"{pav}º pavimento", Codigo = $"{pr.Codigo}-P{pav:00}", Ordem = pav, LocalPaiId = pr.Id, SpeId = p.SpeId };
                lista.Add(pv);
                var seq = 0;
                foreach (var (tip, qtd) in p.MixPorPavimento)
                    for (var i = 0; i < qtd; i++)
                    {
                        seq++;
                        var numero = pav * 100 + seq;
                        var u = new Local
                        {
                            ObraId = obraId, Tipo = TipoUnidade, Nome = $"Apto {numero}", Codigo = $"{pv.Codigo}-{numero}", Ordem = seq, LocalPaiId = pv.Id,
                            SpeId = p.SpeId, TipologiaId = tip.Id, AreaPrivativa = tip.AreaPrivativa, Quantidade = tip.AreaPrivativa, Unidade = "m²"
                        };
                        unidades.Add(u); lista.Add(u);
                    }
            }
            DistribuirFracao(unidades);
        }
        return lista;
    }

    public static void DistribuirFracao(IList<Local> unidades)
    {
        var total = unidades.Sum(u => u.AreaPrivativa ?? 0);
        if (total <= 0 || unidades.Count == 0) return;
        foreach (var u in unidades) u.FracaoIdeal = Math.Round((u.AreaPrivativa ?? 0) / total, 6);
        unidades[^1].FracaoIdeal += 1m - unidades.Sum(u => u.FracaoIdeal ?? 0);
    }

    /// <summary>Conferência: soma da fração ideal das unidades de cada prédio deve ser 100% (±0,0001%).</summary>
    public static IReadOnlyList<string> ConferirFracao(IEnumerable<Local> locais)
    {
        var e = new List<string>();
        var porId = locais.ToDictionary(l => l.Id);
        Local? Predio(Local l) { var x = l; while (x is not null && x.Tipo != TipoPredio) x = x.LocalPaiId is { } pid && porId.TryGetValue(pid, out var pai) ? pai : null; return x; }
        foreach (var g in porId.Values.Where(l => l.Tipo == TipoUnidade).GroupBy(Predio))
        {
            if (g.Key is null) continue;
            var soma = g.Sum(u => u.FracaoIdeal ?? 0);
            if (Math.Abs(soma - 1m) > 0.000001m) e.Add($"{g.Key.Nome}: soma da fração ideal = {soma * 100:0.0000}% (deve ser 100%).");
        }
        return e;
    }
}
