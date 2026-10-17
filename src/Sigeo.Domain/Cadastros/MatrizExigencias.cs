using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>Matriz de exigências: quais documentos pedir para cada tipo de fornecedor (material × serviço, com ou sem funcionários).</summary>
public static class MatrizExigencias
{
    public static bool Aplica(TipoDocumentoExigido t, TipoFornecimento tipo, bool incluirFuncionarios) =>
        t.Ativo && t.AplicaA == AplicaA.Fornecedor && (t.ExigidoPara == TipoFornecimento.Nenhum || (t.ExigidoPara & tipo) != 0) && (!t.DeFuncionario || incluirFuncionarios);

    /// <summary>Competência que deve estar entregue hoje: a do mês anterior, a partir do dia limite (padrão 20).</summary>
    public static DateOnly? CompetenciaExigida(TipoDocumentoExigido t, DateOnly hoje)
    {
        if (t.Frequencia != FrequenciaDocumento.Mensal) return null;
        var limite = Math.Clamp(t.DiaLimite ?? 20, 1, 28);
        var mesAnterior = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1);
        return hoje.Day > limite ? mesAnterior : mesAnterior.AddMonths(-1);
    }
}
