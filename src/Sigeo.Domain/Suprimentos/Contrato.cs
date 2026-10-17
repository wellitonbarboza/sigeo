using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T11 · Contrato de serviço (empreiteiro, laboratório, consultoria, locação crítica). Base da medição.</summary>
public class Contrato : EntidadeDaObra, IHistoricoTemporal
{
    /// <summary>Ex.: CT-2026-041</summary>
    public string Numero { get; set; } = string.Empty;
    public Guid? CentroCustoId { get; set; }
    /// <summary>SPE contratante (CNPJ do contrato e das NFs de medição).</summary>
    public Guid? SpeId { get; set; }
    public Guid? EstabelecimentoFaturamentoId { get; set; }
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public Guid? RequisicaoId { get; set; }
    public Guid? CotacaoId { get; set; }
    /// <summary>Lista CategoriaServico (serviço de obra, laboratório, serviço esp. engenharia, consultoria, locação).</summary>
    public string Categoria { get; set; } = string.Empty;
    public string Objeto { get; set; } = string.Empty;
    public string? RequisitosLaboratorio { get; set; }
    public string? RequisitosDesempenho { get; set; }
    public string? RequisitosEquipamento { get; set; }
    public decimal RetencaoPerc { get; set; }
    public DateOnly VigenciaInicio { get; set; }
    public DateOnly VigenciaFim { get; set; }
    public FormalizacaoContrato Formalizacao { get; set; } = FormalizacaoContrato.ContratoPadrao;
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoDiretoriaPor { get; set; }
    public DateTimeOffset? AprovadoDiretoriaEm { get; set; }
    public DateOnly? AssinaturaEmpresa { get; set; }
    public DateOnly? AssinaturaFornecedor { get; set; }
    public DateTimeOffset? EnviadoFornecedorEm { get; set; }
    public StatusContrato Status { get; set; } = StatusContrato.EmElaboracao;

    // ───── Cobrança recorrente (locação, serviço contínuo, manutenção mensal) ─────
    /// <summary>Gera parcelas automáticas (em vez de medição com avaliação).</summary>
    public bool Recorrente { get; set; }
    public FormaCobranca FormaCobranca { get; set; } = FormaCobranca.Medicao;
    public Periodicidade Periodicidade { get; set; } = Periodicidade.Mensal;
    /// <summary>Mensal: dia de fechamento do período.</summary>
    public int DiaCorte { get; set; } = 30;
    public int DiaVencimento { get; set; } = 10;
    /// <summary>Datas combinadas com o fornecedor ("dd/MM/yyyy;dd/MM/yyyy").</summary>
    public string? DatasCombinadas { get; set; }
    /// <summary>Diferença aceita entre a nota e o previsto (%) sem justificativa.</summary>
    public decimal ToleranciaPerc { get; set; } = 2;
    /// <summary>Ex.: "IGP-M · julho".</summary>
    public string? Reajuste { get; set; }
    /// <summary>Contrato de manutenção: a parcela só é aprovada com a OS da visita do período concluída.</summary>
    public bool ExigeOsPorVisita { get; set; }

    public ICollection<ContratoItem> Itens { get; set; } = [];
    public ICollection<ContratoServico> ServicosVinculados { get; set; } = [];

    public decimal ValorTotal => Itens.Sum(i => i.Total);

    public static bool Contem(string categoria, string trecho) =>
        categoria.Contains(trecho, StringComparison.OrdinalIgnoreCase);

    /// <summary>Conteúdo mínimo por categoria (P.SUP 3.2.2) e regras de formalização.</summary>
    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (FornecedorId == Guid.Empty) e.Add("Informe o fornecedor.");
        if (string.IsNullOrWhiteSpace(Categoria)) e.Add("Informe a categoria.");
        if (string.IsNullOrWhiteSpace(Objeto)) e.Add("Informe o objeto/escopo (escopo claro + especificações + requisitos).");
        if (VigenciaFim < VigenciaInicio) e.Add("Vigência: o fim deve ser posterior ao início.");
        if (Itens.Count == 0) e.Add("Inclua a planilha de itens do contrato (base da medição).");
        if (Contem(Categoria, "laborat") && string.IsNullOrWhiteSpace(RequisitosLaboratorio))
            e.Add("Laboratório: informe os requisitos (acreditação dos ensaios, competência, prazos, normas, equipamentos).");
        if (Contem(Categoria, "especializado") && string.IsNullOrWhiteSpace(RequisitosDesempenho))
            e.Add("Serviço especializado de engenharia: informe os requisitos de desempenho (NBR 15575).");
        if (Contem(Categoria, "loca") && string.IsNullOrWhiteSpace(RequisitosEquipamento))
            e.Add("Locação: informe os requisitos do equipamento (especificação, manutenção, responsabilidades).");
        if (Contem(Categoria, "obra") && ServicosVinculados.Count == 0)
            e.Add("Serviço de obra: vincule as ITS que serão enviadas ao prestador.");
        if (Formalizacao == FormalizacaoContrato.AceiteProposta && !(Contem(Categoria, "consult") || Contem(Categoria, "especializado")))
            e.Add("Aceite de proposta só é permitido para consultoria ou serviço especializado de engenharia.");
        foreach (var i in Itens.Where(i => i.Quantidade <= 0 || i.PrecoUnitario < 0))
            e.Add($"Item {i.Item}: quantidade e preço devem ser positivos.");
        foreach (var i in Itens.Where(i => i.Locais.Count > 0))
        {
            if (i.Locais.Any(l => l.Quantidade < 0)) e.Add($"Item {i.Item}: quantidade por local não pode ser negativa.");
            if (i.Locais.GroupBy(l => l.LocalId).Any(g => g.Count() > 1)) e.Add($"Item {i.Item}: local repetido.");
            if (i.Locais.Sum(l => l.Quantidade) > i.Quantidade + 0.0001m) e.Add($"Item {i.Item}: soma dos locais ({i.Locais.Sum(l => l.Quantidade)}) maior que a quantidade do item ({i.Quantidade}).");
        }
        return e;
    }

    /// <summary>Ativa o contrato: exige aprovação da Diretoria e as duas assinaturas.</summary>
    public void Ativar()
    {
        if (AprovadoDiretoriaEm is null) throw new Aprovacoes.RegraDeNegocioException("Contrato sem aprovação da Diretoria.");
        if (AssinaturaEmpresa is null || AssinaturaFornecedor is null)
            throw new Aprovacoes.RegraDeNegocioException("Informe as assinaturas da empresa e do fornecedor para ativar o contrato.");
        Status = StatusContrato.Ativo;
    }
}
