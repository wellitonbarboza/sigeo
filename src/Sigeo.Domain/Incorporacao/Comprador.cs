using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Comprador (dados pessoais protegidos: só quem tem a tela de vendas vê).</summary>
public class Comprador : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string? Conjuge { get; set; }
    public string? Observacao { get; set; }
}
