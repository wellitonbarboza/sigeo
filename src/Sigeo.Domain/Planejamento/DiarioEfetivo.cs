using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Linha do efetivo: própria (Nome = função, FuncaoObraId) ou de terceiro (Nome = empreiteiro, FornecedorId opcional).</summary>
public class DiarioEfetivo : Entidade
{
    public Guid DiarioId { get; set; }
    public bool Terceiro { get; set; }
    public Guid? FuncaoObraId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}
