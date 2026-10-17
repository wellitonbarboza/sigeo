using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Estabelecimento (CNPJ): matriz, filiais (construtora, pré-moldados, móveis) e SPEs. Eixo "quem fatura" de todo lançamento.
/// Quem fatura os materiais (padrão) e o almoxarifado central são marcados aqui.
/// </summary>
public class Estabelecimento : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public TipoEstabelecimento Tipo { get; set; } = TipoEstabelecimento.Filial;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    /// <summary>Raiz do CNPJ (8 dígitos): mesma raiz = mesma pessoa jurídica (transferência, não venda).</summary>
    public string Raiz { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Endereco { get; set; }
    public string? Atividade { get; set; }
    /// <summary>SPE correspondente (quando Tipo = SPE).</summary>
    public Guid? SpeId { get; set; }
    /// <summary>Padrão do "Faturar para" nas compras de material (ex.: fábrica de pré-moldados).</summary>
    public bool FaturaMateriais { get; set; }
    /// <summary>Recebe as compras no almoxarifado central (FVM) e transfere para as obras/SPEs.</summary>
    public bool AlmoxarifadoCentral { get; set; }
    public string? EnderecoEntrega { get; set; }
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;
}
