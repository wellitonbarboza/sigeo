using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>ITS vinculadas ao contrato (enviadas ao prestador junto com o contrato).</summary>
public class ContratoServico : Entidade
{
    public Guid ContratoId { get; set; }
    public Guid ServicoControladoId { get; set; }
    public ServicoControlado ServicoControlado { get; set; } = null!;
}
