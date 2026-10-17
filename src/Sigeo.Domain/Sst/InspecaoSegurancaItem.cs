using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public class InspecaoSegurancaItem : Entidade
{
    public Guid InspecaoSegurancaId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public ResultadoItem Resultado { get; set; }
    public Gravidade? Gravidade { get; set; }
    public string? Responsavel { get; set; }
    public DateOnly? Prazo { get; set; }
    public string? Observacao { get; set; }
    public bool Corrigido { get; set; }
    public Guid? RncId { get; set; }
}
