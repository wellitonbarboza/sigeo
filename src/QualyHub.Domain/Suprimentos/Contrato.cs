using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;

namespace QualyHub.Domain.Suprimentos;

/// <summary>T11 · Contrato de serviço (empreiteiro, laboratório, consultoria, locação crítica). Base da medição.</summary>
public class Contrato : EntidadeDaObra, IHistoricoTemporal
{
    /// <summary>Ex.: CT-2026-041</summary>
    public string Numero { get; set; } = string.Empty;
    public Guid? CentroCustoId { get; set; }
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

/// <summary>T12 · Item do contrato (planilha de preços — base da medição).</summary>
public class ContratoItem : Entidade
{
    public Guid ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    /// <summary>Ex.: 1.1</summary>
    public string Item { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid? ServicoControladoId { get; set; }
    public ServicoControlado? ServicoControlado { get; set; }
    public string Unidade { get; set; } = "un";
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total => Math.Round(Quantidade * PrecoUnitario, 2);
}

/// <summary>ITS vinculadas ao contrato (enviadas ao prestador junto com o contrato).</summary>
public class ContratoServico : Entidade
{
    public Guid ContratoId { get; set; }
    public Guid ServicoControladoId { get; set; }
    public ServicoControlado ServicoControlado { get; set; } = null!;
}
