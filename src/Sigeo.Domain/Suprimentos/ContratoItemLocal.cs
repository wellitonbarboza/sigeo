using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

public class ContratoItemLocal : Entidade
{
    public Guid ContratoItemId { get; set; }
    public Guid LocalId { get; set; }
    public decimal Quantidade { get; set; }
}
