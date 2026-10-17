using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Almoxarifado: o central da construtora (ligado ao estabelecimento) ou o da obra (um por obra).</summary>
public class Almoxarifado : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public TipoAlmoxarifado Tipo { get; set; } = TipoAlmoxarifado.Obra;
    public Guid? EstabelecimentoId { get; set; }
    public Guid? ObraId { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;
}
