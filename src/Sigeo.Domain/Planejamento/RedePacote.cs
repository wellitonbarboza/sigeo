using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Pacote de serviço da rede (normalmente um serviço do orçamento), com duração por local e nº de equipes.</summary>
public class RedePacote : Entidade
{
    public Guid RedeId { get; set; }
    public int Ordem { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ComposicaoId { get; set; }
    public int DuracaoPorLocal { get; set; } = 1;
    public int Equipes { get; set; } = 1;
    /// <summary>Ajustes do "o que está incluso": por padrão o pacote entra nos locais onde o orçamento tem o serviço (ou em todos, sem serviço).</summary>
    public ICollection<RedePacoteLocal> Ajustes { get; set; } = [];
}
