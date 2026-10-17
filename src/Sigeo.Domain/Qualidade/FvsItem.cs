using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q05 · Resultado de cada item da FVS.</summary>
public class FvsItem : Entidade
{
    public Guid FvsId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public FotoItem Foto { get; set; }
    public bool Bloqueia { get; set; }
    public bool ExigeEquipamento { get; set; }
    public ResultadoItem Resultado { get; set; }
    public decimal? ValorMedido { get; set; }
    public string? Observacao { get; set; }
    public ResultadoItem? ResultadoReinspecao { get; set; }

    public ResultadoItem ResultadoFinal => ResultadoReinspecao ?? Resultado;

    public void AplicarValor()
    {
        if (ValorMedido is null || (ValorMin is null && ValorMax is null)) return;
        Resultado = (ValorMin is null || ValorMedido >= ValorMin) && (ValorMax is null || ValorMedido <= ValorMax) ? ResultadoItem.Conforme : ResultadoItem.NaoConforme;
    }
}
