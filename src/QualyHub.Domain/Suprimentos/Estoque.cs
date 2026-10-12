using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Suprimentos;

// ═══════════════════════ Estoque físico e gerencial (almoxarifado central + almoxarifados das obras) ═══════════════════════

public enum TipoAlmoxarifado { Central = 1, Obra = 2 }

public enum TipoMovimentoEstoque
{
    /// <summary>Entrada pelo recebimento com FVM (disponível ou bloqueada, conforme o lote).</summary>
    Entrada = 1,
    /// <summary>Saída para aplicação (apartamento/local + serviço), avulsa ou por kit.</summary>
    Saida = 2,
    TransferenciaSaida = 3,
    TransferenciaEntrada = 4,
    /// <summary>Sobra devolvida pela frente de serviço.</summary>
    Devolucao = 5,
    AjusteInventario = 6,
    Perda = 7,
    /// <summary>Lote liberado após o ensaio: sai do bloqueado e entra no disponível.</summary>
    Liberacao = 8,
    /// <summary>Saldo inicial (inventário de implantação).</summary>
    Implantacao = 9,
    /// <summary>Entrega de EPI ao colaborador.</summary>
    EntregaEpi = 10,
}

public enum TipoInventario { Implantacao = 1, Ciclico = 2, Geral = 3 }
public enum StatusInventario { EmContagem = 1, AguardandoAprovacao = 2, Aprovado = 3, Cancelado = 4 }
public enum OrigemKit { Automatico = 1, Manual = 2 }

/// <summary>Almoxarifado: o central da construtora (ligado ao estabelecimento) ou o da obra (um por obra).</summary>
public class Almoxarifado : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public TipoAlmoxarifado Tipo { get; set; } = TipoAlmoxarifado.Obra;
    public Guid? EstabelecimentoId { get; set; }
    public Guid? ObraId { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Saldo do produto no almoxarifado (atualizado junto com cada movimento) + mínimo/ponto de pedido/máximo.</summary>
public class SaldoEstoque : EntidadeAuditavel
{
    public Guid AlmoxarifadoId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Disponivel { get; set; }
    /// <summary>Material em quarentena até o ensaio (não pode sair).</summary>
    public decimal Bloqueado { get; set; }
    /// <summary>Custo médio ponderado do almoxarifado.</summary>
    public decimal CustoMedio { get; set; }
    public decimal Minimo { get; set; }
    public decimal PontoPedido { get; set; }
    public decimal Maximo { get; set; }
    public DateTimeOffset? UltimoMovimento { get; set; }

    public decimal Valor => Math.Round((Disponivel + Bloqueado) * CustoMedio, 2);
    public bool AbaixoDoMinimo => Minimo > 0 && Disponivel <= Minimo;
    public bool NoPontoDePedido => PontoPedido > 0 && Disponivel <= PontoPedido;
}

/// <summary>Ficha de estoque (kardex): cada entrada, saída, transferência ou ajuste. Nunca é alterado — corrige-se com outro movimento.</summary>
public class MovimentoEstoque : EntidadeAuditavel
{
    public Guid AlmoxarifadoId { get; set; }
    public Guid ProdutoId { get; set; }
    public DateTimeOffset Data { get; set; }
    public TipoMovimentoEstoque Tipo { get; set; }
    /// <summary>Com sinal: positivo entra, negativo sai.</summary>
    public decimal Quantidade { get; set; }
    /// <summary>Movimento no saldo bloqueado (quarentena).</summary>
    public bool Bloqueado { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal Valor { get; set; }
    public decimal SaldoApos { get; set; }
    /// <summary>Documento de origem legível (ex.: "Recebimento 219", "Transferência 31").</summary>
    public string Documento { get; set; } = string.Empty;
    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }
    /// <summary>Apartamento / local onde foi aplicado (saída).</summary>
    public Guid? LocalId { get; set; }
    /// <summary>Serviço (composição) em que foi aplicado (saída).</summary>
    public Guid? ComposicaoId { get; set; }
    public Guid? KitId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string? EntreguePara { get; set; }
    public string? Lote { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Inventário (implantação, cíclico ou geral). Diferenças viram ajuste depois da aprovação.</summary>
public class InventarioEstoque : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid AlmoxarifadoId { get; set; }
    public TipoInventario Tipo { get; set; } = TipoInventario.Ciclico;
    public DateOnly Data { get; set; }
    public StatusInventario Status { get; set; } = StatusInventario.EmContagem;
    public string? Observacao { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public ICollection<InventarioItem> Itens { get; set; } = [];
}

public class InventarioItem : Entidade
{
    public Guid InventarioId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal SaldoSistema { get; set; }
    public decimal? Contado { get; set; }
    /// <summary>Custo informado na implantação (no cíclico vale o custo médio).</summary>
    public decimal? CustoUnitario { get; set; }
    public string? Observacao { get; set; }
    public decimal Diferenca => (Contado ?? SaldoSistema) - SaldoSistema;
}

/// <summary>
/// Kit de material. Automático: definido por etapa (grupo do serviço) e/ou disciplina — as quantidades saem do Orçamento por Local
/// de cada apartamento na hora da entrega. Manual: lista fixa (EPI de admissão, kit de ferramentas…).
/// </summary>
public class KitMaterial : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public OrigemKit Origem { get; set; } = OrigemKit.Automatico;
    /// <summary>Obra do kit automático (o orçamento é da obra). Kit manual pode valer para todas.</summary>
    public Guid? ObraId { get; set; }
    /// <summary>Etapa = grupo do serviço (EAP).</summary>
    public string? Etapa { get; set; }
    public string? Disciplina { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<KitMaterialItem> Itens { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome do kit.");
        if (Origem == OrigemKit.Automatico && ObraId is null) e.Add("Kit automático: informe a obra (o kit sai do orçamento dela).");
        if (Origem == OrigemKit.Automatico && string.IsNullOrWhiteSpace(Etapa) && string.IsNullOrWhiteSpace(Disciplina)) e.Add("Kit automático: informe a etapa e/ou a disciplina.");
        if (Origem == OrigemKit.Manual && Itens.Count == 0) e.Add("Kit manual: inclua os itens.");
        if (Itens.Any(i => i.Quantidade <= 0)) e.Add("Quantidade dos itens deve ser maior que zero.");
        if (Itens.GroupBy(i => i.ProdutoId).Any(g => g.Count() > 1)) e.Add("Produto repetido no kit.");
        return e;
    }
}

public class KitMaterialItem : Entidade
{
    public Guid KitId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
}

/// <summary>Linha de necessidade de material (kit de um local, ou soma de vários).</summary>
public sealed record NecessidadeMaterial(Guid ProdutoId, Guid? ComposicaoId, decimal Quantidade, decimal CustoOrcado);

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
