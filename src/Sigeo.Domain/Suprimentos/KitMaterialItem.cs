using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public class KitMaterialItem : Entidade
{
    public Guid KitId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
}
