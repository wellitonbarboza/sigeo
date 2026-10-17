using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Fluxo entre CNPJs (ex.: Fábrica → SPE, venda a custo padrão). Base para o mapa do ecossistema e para a contabilidade.</summary>
public class FluxoEstabelecimento : EntidadeAuditavel
{
    public Guid OrigemId { get; set; }
    public Guid DestinoId { get; set; }
    public TipoFluxo Tipo { get; set; } = TipoFluxo.Transferencia;
    /// <summary>Base do valor: custo padrão, custo real, % sobre custo, taxa fixa…</summary>
    public string Base { get; set; } = "Custo padrão";
    public string Situacao { get; set; } = "A definir";
    public string? Descricao { get; set; }
}
