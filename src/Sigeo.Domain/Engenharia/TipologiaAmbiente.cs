using System.Globalization;
using System.Text;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Engenharia;

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
