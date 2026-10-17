using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Carteira de compras de um comprador: categorias que compra, obras, substituto e ausências.</summary>
public class CarteiraComprador : EntidadeAuditavel
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? SubstitutoUsuarioId { get; set; }
    public string? SubstitutoNome { get; set; }
    /// <summary>Opcional: acima deste número de lotes abertos o gestor é avisado.</summary>
    public int? LimiteCarga { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<CarteiraCategoria> Categorias { get; set; } = [];
    public ICollection<AusenciaComprador> Ausencias { get; set; } = [];

    public bool AusenteEm(DateOnly dia) => Ausencias.Any(a => a.Inicio <= dia && a.Fim >= dia);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (UsuarioId == Guid.Empty) e.Add("Escolha o usuário comprador.");
        if (SubstitutoUsuarioId == UsuarioId && UsuarioId != Guid.Empty) e.Add("O substituto precisa ser outra pessoa.");
        if (LimiteCarga is < 1) e.Add("Limite de carga precisa ser maior que zero.");
        foreach (var c in Categorias)
            if (string.IsNullOrWhiteSpace(c.Grupo) && string.IsNullOrWhiteSpace(c.CategoriaServico)) e.Add("Categoria sem grupo de produto nem categoria de serviço.");
        foreach (var a in Ausencias)
            if (a.Fim < a.Inicio) e.Add($"Ausência de {a.Inicio:dd/MM/yyyy}: fim antes do início.");
        return e;
    }
}
