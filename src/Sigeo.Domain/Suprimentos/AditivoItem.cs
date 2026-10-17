using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

public class AditivoItem : Entidade
{
    public Guid AditivoId { get; set; }
    public Guid? ContratoItemId { get; set; }
    public string? Item { get; set; }
    public string? Descricao { get; set; }
    public string? Unidade { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public decimal QtdOriginal { get; set; }
    public decimal QtdDelta { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal PrecoOriginal { get; set; }
    public decimal ValorDelta => Math.Round((QtdOriginal + QtdDelta) * PrecoUnitario - QtdOriginal * PrecoOriginal, 2);
}
