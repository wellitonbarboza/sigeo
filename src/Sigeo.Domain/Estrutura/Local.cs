using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Hierarquia física da obra: Condomínio (SPE) › Torre › Pavimento › Apartamento › Cômodo, com áreas comuns
/// no pavimento, na torre e no condomínio. Gerada pelas tipologias; usada no orçamento, nas FVS, na requisição e na medição.
/// </summary>
public class Local : EntidadeDaObra
{
    public Guid? LocalPaiId { get; set; }
    public Local? LocalPai { get; set; }
    public string Tipo { get; set; } = "Pavimento"; // Condomínio, Torre, Pavimento, Apartamento, Cômodo, Área comum
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Unidade { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
    /// <summary>SPE dona (prédio e tudo abaixo dele). Custo direto do local vai para ela.</summary>
    public Guid? SpeId { get; set; }
    /// <summary>Unidade: tipologia e área privativa (cópia na geração, pode ser ajustada).</summary>
    public Guid? TipologiaId { get; set; }
    public decimal? AreaPrivativa { get; set; }
    /// <summary>Fração ideal do apartamento no condomínio (0–1). Oficial = memorial de incorporação.</summary>
    public decimal? FracaoIdeal { get; set; }
    /// <summary>Cômodo/área comum: categoria da lista padrão (Sala, Dormitório, BWC…).</summary>
    public string? Categoria { get; set; }
    /// <summary>Ambiente-modelo da tipologia que gerou o local (as medidas são copiadas e podem ser sincronizadas).</summary>
    public Guid? AmbienteModeloId { get; set; }
    public decimal? AreaPiso { get; set; }
    public decimal? Perimetro { get; set; }
    public decimal? PeDireito { get; set; }
    /// <summary>Área de parede líquida (perímetro × pé-direito − vãos).</summary>
    public decimal? AreaParede { get; set; }
    public ICollection<Local> Filhos { get; set; } = [];
}
