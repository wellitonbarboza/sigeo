using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S13 — Ocorrências ═══════════════════════

public class Ocorrencia : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public DateTimeOffset DataHora { get; set; }
    public TipoOcorrencia Tipo { get; set; }
    public Guid LocalId { get; set; }
    public Guid? FornecedorId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int DiasPerdidos { get; set; }
    public string? Cat { get; set; }
    public string? ArquivoRestritoBlob { get; set; }
    public Guid? PermissaoTrabalhoId { get; set; }
    public Guid? InspecaoSegurancaId { get; set; }
    public Guid? AcaoCorretivaId { get; set; }
    public StatusOcorrencia Status { get; set; } = StatusOcorrencia.Registrada;

    public bool Acidente => Tipo is TipoOcorrencia.AcidenteSemAfastamento or TipoOcorrencia.AcidenteComAfastamento;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a ocorrência (sem dados médicos).");
        if (Tipo == TipoOcorrencia.AcidenteComAfastamento && DiasPerdidos <= 0) e.Add("Acidente com afastamento: informe os dias perdidos.");
        if (Acidente && string.IsNullOrWhiteSpace(Cat)) e.Add("Acidente: informe a referência da CAT.");
        if (Status == StatusOcorrencia.Encerrada && Acidente && AcaoCorretivaId is null) e.Add("Acidente só encerra com investigação (ação corretiva).");
        return e;
    }
}
