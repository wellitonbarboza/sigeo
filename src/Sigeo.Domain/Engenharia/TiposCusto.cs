using Sigeo.Domain.Common;

namespace Sigeo.Domain.Engenharia;

/// <summary>Rótulos do tipo de custo nas telas.</summary>
public static class TiposCusto
{
    public static string Nome(TipoCusto t) => t switch
    {
        TipoCusto.Material => "Material", TipoCusto.Equipamento => "Equipamento", TipoCusto.MaoObra => "Mão de obra",
        TipoCusto.Servico => "Serviço", TipoCusto.Frete => "Frete", _ => "Outros"
    };

    public static readonly TipoCusto[] Todos = Enum.GetValues<TipoCusto>();
}
