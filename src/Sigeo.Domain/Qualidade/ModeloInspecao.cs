using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Q01/Q02/Q12 — Modelos de inspeção ═══════════════════════

/// <summary>Q01 · Modelo de FVS (serviço), FVM (material) ou checklist SST. Segue a revisão do documento (ITS / especificação).</summary>
public class ModeloInspecao : EntidadeAuditavel, IHistoricoTemporal
{
    public TipoModelo Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public Guid? ServicoControladoId { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public string? NormaReferencia { get; set; }
    public string? Amostragem { get; set; }
    public bool QuarentenaAteEnsaio { get; set; }
    public bool RastrearAplicacao { get; set; }
    public Guid? DocumentoId { get; set; }
    public Guid? RevisaoDocumentoId { get; set; }
    public int Revisao { get; set; }
    public string? MomentoInspecao { get; set; }
    /// <summary>FVS: pré-requisitos para a execução do serviço. FVM: orientações de armazenamento. Mostrado ao inspetor.</summary>
    public string? Orientacoes { get; set; }
    public string? NivelLocal { get; set; }
    public bool LiberaMedicao { get; set; } = true;
    public Guid? ServicoSeguinteId { get; set; }
    public SituacaoModelo Situacao { get; set; } = SituacaoModelo.Rascunho;
    public ICollection<ItemVerificacao> Itens { get; set; } = [];
    public ICollection<EnsaioModelo> Ensaios { get; set; } = [];

    public string Rotulo => $"R{Revisao:00}";

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Codigo)) e.Add("Informe o código do modelo.");
        if (Tipo == TipoModelo.FVS && ServicoControladoId is null) e.Add("FVS: informe o serviço controlado (ITS).");
        if (Tipo == TipoModelo.FVM && MaterialControladoId is null) e.Add("FVM: informe o material controlado.");
        if (Tipo == TipoModelo.FVM && string.IsNullOrWhiteSpace(NormaReferencia)) e.Add("FVM: informe a norma / especificação.");
        if (Tipo == TipoModelo.FVM && string.IsNullOrWhiteSpace(Amostragem)) e.Add("FVM: informe a amostragem.");
        if (Itens.Count == 0) e.Add("Inclua ao menos um item de verificação.");
        foreach (var i in Itens)
        {
            if (string.IsNullOrWhiteSpace(i.Descricao)) e.Add($"Item {i.Ordem}: descrição obrigatória.");
            if (string.IsNullOrWhiteSpace(i.Criterio)) e.Add($"Item {i.Ordem}: critério de aceitação obrigatório.");
            if (i.ValorMin is { } mi && i.ValorMax is { } ma && ma < mi) e.Add($"Item {i.Ordem}: valor máximo menor que o mínimo.");
        }
        foreach (var x in Ensaios.Where(x => x.Momento == MomentoEnsaio.Posterior && string.IsNullOrWhiteSpace(x.PrazoDias)))
            e.Add($"Ensaio '{x.Ensaio}': informe a idade / prazo em dias.");
        return e;
    }

    /// <summary>Nova revisão do documento-base: modelo vigente entra em revisão (FVS novas só com o modelo revisado).</summary>
    public void DocumentoRevisado()
    {
        if (Situacao == SituacaoModelo.Vigente) Situacao = SituacaoModelo.EmRevisao;
    }

    public void Publicar(Guid? revisaoDocumentoId)
    {
        ValidacaoFalha.Se(Validar());
        if (Situacao is SituacaoModelo.EmRevisao) Revisao++;
        RevisaoDocumentoId = revisaoDocumentoId ?? RevisaoDocumentoId;
        Situacao = SituacaoModelo.Vigente;
    }
}
