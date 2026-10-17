using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Vínculo entre pacotes de redes diferentes (ex.: alvenaria do pav. N após estrutura do pav. N+2; fachada após a alvenaria).
/// Filho dos pacotes (sai junto quando o pacote/rede sai), por isso não tem exclusão lógica; acesso sempre pela obra.
/// </summary>
public class VinculoRede : Entidade
{
    public Guid ObraId { get; set; }
    public Guid PacoteOrigemId { get; set; }
    public Guid PacoteDestinoId { get; set; }
    public ModoVinculoRede Modo { get; set; } = ModoVinculoRede.PorPavimento;
    public int Deslocamento { get; set; }
    public int Lag { get; set; }
}
