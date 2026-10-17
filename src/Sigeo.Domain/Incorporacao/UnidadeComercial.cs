using Sigeo.Domain.Common;
using Sigeo.Domain.Financeiro;

namespace Sigeo.Domain.Incorporacao;

/// <summary>Unidade à venda: o apartamento/casa (Local) com preço de tabela e situação comercial.</summary>
public class UnidadeComercial : EntidadeDaObra
{
    public Guid LocalId { get; set; }
    public decimal PrecoTabela { get; set; }
    public SituacaoUnidade Situacao { get; set; } = SituacaoUnidade.Disponivel;
    public string? Vagas { get; set; }
    public string? Observacao { get; set; }
}
