using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>T09 · Qualificação do fornecedor (um registro por fornecedor × escopo × data). Substitui a planilha de qualificados.</summary>
public class FornecedorQualificacao : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public TipoFornecimento TipoFornecimento { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
    /// <summary>Item da lista CriterioQualificacao (histórico, PSQ, SBAC, documentos, visita, amostra, indicação, laboratório).</summary>
    public string CriterioQualificacao { get; set; } = string.Empty;
    public SituacaoPsq Psq { get; set; }
    public DateOnly? DataConsultaPsq { get; set; }
    public string? CertificadoSbac { get; set; }
    public EnquadramentoLaboratorio LaboratorioEnquadramento { get; set; }
    public string? AvaliadorLaboratorio { get; set; }
    public DateOnly DataQualificacao { get; set; }
    public DateOnly? DataValidade { get; set; }
    public ResultadoQualificacao Resultado { get; set; } = ResultadoQualificacao.EmQualificacao;
    public Guid? ResponsavelUsuarioId { get; set; }
    public string? ResponsavelNome { get; set; }
    /// <summary>Qualificado pela migração do histórico (planilha anterior ao sistema).</summary>
    public bool QualificadoPorHistorico { get; set; }
    public string? Observacao { get; set; }

    /// <summary>Validade automática: material +N meses (reconsultar a cada compra após o prazo); laboratório c)–f) +12 meses.</summary>
    public void CalcularValidade(ParametrosVigentes p)
    {
        if (LaboratorioEnquadramento == EnquadramentoLaboratorio.A_Acreditado)
            DataValidade = null; // acreditação CGCRE: controla pelo certificado nos documentos
        else if (LaboratorioEnquadramento >= EnquadramentoLaboratorio.C_Avaliado)
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.RequalifLaboratorioMeses));
        else if (TipoFornecimento.HasFlag(TipoFornecimento.Material))
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.ValidadeQualifMaterialMeses));
        else
            DataValidade = DataQualificacao.AddMonths(p.Int(Parametros.ValidadeQualifServicoMeses));
    }

    /// <summary>Validações de preenchimento (retorna lista de erros legíveis).</summary>
    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (FornecedorId == Guid.Empty) e.Add("Informe o fornecedor.");
        if (TipoFornecimento == TipoFornecimento.Nenhum) e.Add("Informe o tipo de fornecimento.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.Material) && MaterialControladoId is null) e.Add("Informe o escopo – material controlado.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.ServicoObra) && ServicoControladoId is null) e.Add("Informe o escopo – serviço (ITS).");
        if (string.IsNullOrWhiteSpace(CriterioQualificacao)) e.Add("Informe o critério de qualificação.");
        if (Psq == SituacaoPsq.NaoConforme && Resultado == ResultadoQualificacao.Qualificado)
            e.Add("Fornecedor 'Não conforme' no PSQ não pode ser qualificado (compra vedada).");
        if (Psq is SituacaoPsq.Qualificado or SituacaoPsq.NaoConforme && DataConsultaPsq is null) e.Add("Informe a data da consulta ao PSQ/INMETRO.");
        if (TipoFornecimento.HasFlag(TipoFornecimento.Laboratorio))
        {
            if (LaboratorioEnquadramento == EnquadramentoLaboratorio.NaoSeAplica) e.Add("Informe o enquadramento do laboratório.");
            if (LaboratorioEnquadramento >= EnquadramentoLaboratorio.C_Avaliado && string.IsNullOrWhiteSpace(AvaliadorLaboratorio))
                e.Add("Laboratório não acreditado: informe o avaliador (treinamento em NBR ISO/IEC 17025).");
        }
        if (ResponsavelUsuarioId is null && string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável.");
        return e;
    }
}
