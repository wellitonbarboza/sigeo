using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q02 · Item de verificação do modelo.</summary>
public class ItemVerificacao : Entidade
{
    public Guid ModeloInspecaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
    public bool ExigeEquipamento { get; set; }
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public FotoItem Foto { get; set; } = FotoItem.SoEmNaoConformidade;
    public bool Bloqueia { get; set; } = true;
    public string? Referencia { get; set; } // SST: NR aplicável

    /// <summary>Com mínimo/máximo definidos, o valor medido decide sozinho.</summary>
    public ResultadoItem? AvaliarValor(decimal? medido)
    {
        if (medido is null || (ValorMin is null && ValorMax is null)) return null;
        return (ValorMin is null || medido >= ValorMin) && (ValorMax is null || medido <= ValorMax) ? ResultadoItem.Conforme : ResultadoItem.NaoConforme;
    }
}
