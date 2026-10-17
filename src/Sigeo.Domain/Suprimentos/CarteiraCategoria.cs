using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Categoria da carteira: grupo (e subgrupo opcional) de produto, ou categoria de serviço; obra opcional (vazio = todas).</summary>
public class CarteiraCategoria : Entidade
{
    public Guid CarteiraId { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? CategoriaServico { get; set; }
    public Guid? ObraId { get; set; }

    public string Rotulo => !string.IsNullOrWhiteSpace(CategoriaServico) ? $"Serviço › {CategoriaServico}"
        : string.IsNullOrWhiteSpace(SubGrupo) ? $"{Grupo} · grupo inteiro" : $"{Grupo} › {SubGrupo}";
}
