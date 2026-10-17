using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Serviço controlado (ITS) executado pelo fornecedor.</summary>
public class FornecedorServico : Entidade
{
    public Guid FornecedorId { get; set; }
    public Guid ServicoControladoId { get; set; }
}
