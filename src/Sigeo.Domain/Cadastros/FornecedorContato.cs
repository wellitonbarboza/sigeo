using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>T08 · Fornecedor (dados da Receita + tipo de fornecimento + status de qualificação).</summary>
/// <summary>Contato do fornecedor (comercial, financeiro, técnico, SST…).</summary>
public class FornecedorContato : Entidade
{
    public Guid FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public bool Principal { get; set; }
    /// <summary>Recebe os avisos de vencimento e de documentação do portal.</summary>
    public bool RecebeAvisos { get; set; }
}
