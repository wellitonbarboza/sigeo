using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Equipe de mão de obra própria (ex.: Equipe de alvenaria 1). Os membros são colaboradores próprios do cadastro de pessoas.</summary>
public class Equipe : EntidadeDaObra
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Encarregado { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<EquipeMembro> Membros { get; set; } = [];
}
