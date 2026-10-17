using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Regras do estoque (funções puras, testáveis).</summary>
public static class RegrasEstoque
{
    /// <summary>
    /// Aplica um movimento ao saldo: atualiza disponível/bloqueado e o custo médio (só entradas com custo mudam a média).
    /// Não deixa o saldo ficar negativo. Devolve o saldo do balde movimentado após o movimento.
    /// </summary>
    public static decimal Aplicar(SaldoEstoque s, MovimentoEstoque m)
    {
        if (m.Quantidade == 0) throw new RegraDeNegocioException("Movimento com quantidade zero.");
        if (m.Tipo == TipoMovimentoEstoque.Liberacao)
        {
            var q = Math.Abs(m.Quantidade);
            if (q > s.Bloqueado + 0.00001m) throw new RegraDeNegocioException($"Liberação maior que o saldo bloqueado ({s.Bloqueado:0.###}).");
            s.Bloqueado -= q; s.Disponivel += q;
            m.CustoUnitario = s.CustoMedio; m.Valor = Math.Round(q * s.CustoMedio, 2); m.SaldoApos = s.Disponivel;
            return s.Disponivel;
        }
        var atual = m.Bloqueado ? s.Bloqueado : s.Disponivel;
        if (m.Quantidade > 0)
        {
            var total = s.Disponivel + s.Bloqueado;
            var custo = m.CustoUnitario > 0 ? m.CustoUnitario : s.CustoMedio;
            if (total + m.Quantidade > 0)
                s.CustoMedio = Math.Round((total * s.CustoMedio + m.Quantidade * custo) / (total + m.Quantidade), 4);
            m.CustoUnitario = custo;
        }
        else
        {
            if (atual + m.Quantidade < -0.00001m)
                throw new RegraDeNegocioException($"Saldo insuficiente{(m.Bloqueado ? " bloqueado" : "")}: há {atual:0.###} e a saída é de {-m.Quantidade:0.###}.");
            m.CustoUnitario = s.CustoMedio;
        }
        var novo = atual + m.Quantidade;
        if (m.Bloqueado) s.Bloqueado = novo; else s.Disponivel = novo;
        m.Valor = Math.Round(m.Quantidade * m.CustoUnitario, 2);
        m.SaldoApos = novo;
        s.UltimoMovimento = m.Data;
        return novo;
    }

    /// <summary>Mínimo e ponto de pedido sugeridos: consumo médio diário × prazo de entrega (+50% de segurança no ponto de pedido).</summary>
    public static (decimal Minimo, decimal PontoPedido, decimal Maximo) Sugerir(decimal consumoPeriodo, int diasPeriodo, int leadTimeDias)
    {
        if (diasPeriodo <= 0 || consumoPeriodo <= 0) return (0, 0, 0);
        var porDia = consumoPeriodo / diasPeriodo;
        var prazo = Math.Max(leadTimeDias, 1);
        var minimo = Math.Ceiling(porDia * prazo);
        var pp = Math.Ceiling(minimo * 1.5m);
        return (minimo, pp, Math.Ceiling(pp + porDia * 30));
    }

    /// <summary>Quantidade sugerida para repor até o máximo (ou 1,5 × ponto de pedido).</summary>
    public static decimal Reposicao(SaldoEstoque s) =>
        Math.Max(0, (s.Maximo > 0 ? s.Maximo : Math.Ceiling(s.PontoPedido * 1.5m)) - s.Disponivel);

    /// <summary>Lote em quarentena/verificação entra bloqueado; liberado ou aceito sob concessão entra disponível; devolvido não entra.</summary>
    public static bool? EntraBloqueado(StatusLote lote) => lote switch
    {
        StatusLote.Devolvido => null,
        StatusLote.Liberado or StatusLote.AceitoSobConcessao => false,
        _ => true,
    };

    /// <summary>Soma necessidades (vários apartamentos) por produto.</summary>
    public static List<NecessidadeMaterial> Somar(IEnumerable<NecessidadeMaterial> linhas) =>
        [.. linhas.GroupBy(l => l.ProdutoId).Select(g => new NecessidadeMaterial(g.Key, null, g.Sum(x => x.Quantidade), g.Sum(x => x.CustoOrcado)))];
}
