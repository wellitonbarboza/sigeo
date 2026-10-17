using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Produto / genérico do catálogo de compras (herda "controlado" do material controlado vinculado).</summary>
public class Produto : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    /// <summary>Natureza do custo (material, equipamento, mão de obra, serviço, frete, outros) — curva ABC e custo por tipo.</summary>
    public TipoCusto? TipoCusto { get; set; }
    /// <summary>Descrição completa (especificação resumida). A "genérica" é o Nome — nunca com marca.</summary>
    public string? DescricaoCompleta { get; set; }
    /// <summary>NCM (8 dígitos) — casa o produto com os itens da NF-e.</summary>
    public string? Ncm { get; set; }
    public string? Marca { get; set; }
    /// <summary>Referência / código do fabricante.</summary>
    public string? Referencia { get; set; }
    public string? Ean { get; set; }
    /// <summary>Quantidade por embalagem (caixa, pacote, fardo).</summary>
    public decimal? QuantidadePorEmbalagem { get; set; }
    public string? OrigemSistema { get; set; }
    /// <summary>Código antes da última recodificação (provisório → definitivo, ou reclassificação antes do 1º uso).</summary>
    public string? CodigoAnterior { get; set; }
    /// <summary>Grupo "A Cadastrar" (ou vazio) = produto criado pela NF/importação que ainda precisa ser classificado.</summary>
    public bool AClassificar => string.IsNullOrWhiteSpace(Grupo) || Grupo.Equals("A Cadastrar", StringComparison.OrdinalIgnoreCase);
    /// <summary>Prazo de fornecimento (lead time) usado no cronograma de suprimentos.</summary>
    public int LeadTimeDias { get; set; }
    /// <summary>Preço de referência (tabela interna/orçamento da obra) — base da "economia vs. referência" na cotação.</summary>
    public decimal? PrecoReferencia { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public MaterialControlado? MaterialControlado { get; set; }
    public bool Ativo { get; set; } = true;
    /// <summary>Insumo antigo substituído pelo padronizado equivalente: fica inativo e os próximos usos vão para este.</summary>
    public Guid? SubstituidoPorId { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}
