using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Apropriação: horas da equipe/colaborador num serviço × local, num dia (e a quantidade executada, quando informada).</summary>
public class Apropriacao : EntidadeDaObra
{
    public DateOnly Data { get; set; }
    public Guid? EquipeId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid ComposicaoId { get; set; }
    public Guid LocalId { get; set; }
    public decimal Horas { get; set; }
    public decimal? QuantidadeExecutada { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Agrupa as linhas criadas no mesmo lançamento (um lançamento pode ratear horas em vários locais).</summary>
    public Guid Lancamento { get; set; }
}
