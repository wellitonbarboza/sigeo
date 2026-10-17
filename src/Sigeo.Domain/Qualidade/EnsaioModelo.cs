using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

/// <summary>Q12 · Ensaio de recebimento previsto no modelo de FVM.</summary>
public class EnsaioModelo : Entidade
{
    public Guid ModeloInspecaoId { get; set; }
    public string Ensaio { get; set; } = string.Empty;
    public string? Norma { get; set; }
    public MomentoEnsaio Momento { get; set; } = MomentoEnsaio.NaChegada;
    public string? PrazoDias { get; set; }
    public string Frequencia { get; set; } = string.Empty;
    public ExecutanteEnsaio Executante { get; set; } = ExecutanteEnsaio.Obra;
    public string Criterio { get; set; } = string.Empty;
    public AcaoReprovacaoEnsaio AcaoReprovacao { get; set; } = AcaoReprovacaoEnsaio.AbrirRnc;

    /// <summary>Maior idade informada (ex.: "7 e 28" → 28).</summary>
    public int MaiorPrazo() =>
        (PrazoDias ?? "").Split([' ', ',', ';', '/', 'e'], StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => int.TryParse(x, out var n) ? n : 0).DefaultIfEmpty(0).Max();
}
