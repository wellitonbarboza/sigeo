using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Quantidade do item da requisição em cada local (vem da matriz serviço × local).</summary>
public class RequisicaoItemLocal : Entidade
{
    public Guid RequisicaoItemId { get; set; }
    public Guid LocalId { get; set; }
    public Guid? ComposicaoId { get; set; }
    public decimal Quantidade { get; set; }
}
