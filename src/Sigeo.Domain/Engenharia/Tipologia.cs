using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

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
