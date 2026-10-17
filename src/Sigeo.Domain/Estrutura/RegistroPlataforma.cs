using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Registro das ações da plataforma (criar/suspender construtora, dar acesso, suporte). Fora do filtro de empresa.</summary>
public class RegistroPlataforma : Entidade
{
    public DateTimeOffset Quando { get; set; }
    public string Quem { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public Guid? EmpresaId { get; set; }
    public string? Empresa { get; set; }
    public string? Detalhe { get; set; }
}
