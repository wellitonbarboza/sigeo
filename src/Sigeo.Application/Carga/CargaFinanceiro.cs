using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Financeiro;
using Sigeo.Domain.Incorporacao;

namespace Sigeo.Application.Carga;

/// <summary>
/// Carga genérica do Financeiro e da Incorporação: plano de contas gerencial padrão (só empresa — sem contas pessoais),
/// condições de pagamento usuais e alíquotas do RET. Tudo editável; só acrescenta o que falta.
/// </summary>
public static class CargaFinanceiro
{
    public static readonly (string Codigo, string Nome, NaturezaConta Natureza)[] PlanoContas =
    [
        ("1", "Receitas", NaturezaConta.Receita),
        ("1.1", "Vendas de unidades", NaturezaConta.Receita), ("1.2", "Correção monetária e juros de vendas", NaturezaConta.Receita),
        ("1.3", "Prestação de serviços", NaturezaConta.Receita), ("1.4", "Outras receitas", NaturezaConta.Receita),
        ("1.5", "Aportes e adiantamentos recebidos", NaturezaConta.Receita),
        ("2", "Custos diretos de obra", NaturezaConta.Despesa),
        ("2.1", "Mão de obra", NaturezaConta.Despesa), ("2.2", "Terreno", NaturezaConta.Despesa), ("2.3", "Materiais", NaturezaConta.Despesa),
        ("2.4", "Serviços de terceiros", NaturezaConta.Despesa), ("2.5", "Equipamentos (locação e aquisição)", NaturezaConta.Despesa), ("2.6", "Fretes", NaturezaConta.Despesa),
        ("3", "Custos indiretos de obra", NaturezaConta.Despesa),
        ("3.1", "Projetos, ARTs e licenças", NaturezaConta.Despesa), ("3.2", "Equipe técnica e administrativa da obra", NaturezaConta.Despesa),
        ("3.3", "Canteiro e instalações provisórias", NaturezaConta.Despesa), ("3.4", "Segurança e medicina do trabalho", NaturezaConta.Despesa),
        ("3.5", "Seguros e vigilância", NaturezaConta.Despesa), ("3.6", "Consumos (energia, água, internet)", NaturezaConta.Despesa),
        ("3.7", "Controle tecnológico", NaturezaConta.Despesa), ("3.8", "Despesas administrativas da obra", NaturezaConta.Despesa),
        ("4", "Custos de comercialização", NaturezaConta.Despesa),
        ("4.1", "Comissões de venda", NaturezaConta.Despesa), ("4.2", "Marketing e publicidade", NaturezaConta.Despesa),
        ("4.3", "Stand de vendas e decorado", NaturezaConta.Despesa), ("4.4", "Assessoria jurídica e cartório de vendas", NaturezaConta.Despesa),
        ("5", "Despesas administrativas", NaturezaConta.Despesa),
        ("5.1", "Pessoal administrativo e pró-labore", NaturezaConta.Despesa), ("5.2", "Honorários e consultorias", NaturezaConta.Despesa),
        ("5.3", "Escritório e material gráfico", NaturezaConta.Despesa), ("5.4", "Manutenção e veículos", NaturezaConta.Despesa),
        ("5.5", "Outras despesas administrativas", NaturezaConta.Despesa),
        ("6", "Tributos", NaturezaConta.Despesa),
        ("6.1", "RET (IRPJ, CSLL, PIS/CBS, COFINS/IBS)", NaturezaConta.Despesa), ("6.2", "INSS e encargos", NaturezaConta.Despesa),
        ("6.3", "FGTS e DAS", NaturezaConta.Despesa), ("6.4", "Outros impostos e taxas", NaturezaConta.Despesa),
        ("7", "Resultado financeiro", NaturezaConta.Despesa),
        ("7.1", "Juros e multas pagos", NaturezaConta.Despesa), ("7.2", "Tarifas bancárias", NaturezaConta.Despesa),
        ("7.3", "Empréstimos e financiamentos (amortização)", NaturezaConta.Despesa), ("7.4", "Rendimentos de aplicação", NaturezaConta.Receita),
        ("7.5", "Investimentos", NaturezaConta.Despesa),
        ("8", "Distratos e devoluções", NaturezaConta.Despesa), ("8.1", "Devolução a compradores", NaturezaConta.Despesa),
    ];

    public static readonly (string Nome, decimal Entrada, string Prazos)[] Condicoes =
    [
        ("À vista", 0, "0"), ("7 dias", 0, "7"), ("14 dias", 0, "14"), ("21 dias", 0, "21"), ("28 dias", 0, "28"), ("30 dias", 0, "30"),
        ("28/56", 0, "28;56"), ("30/60", 0, "30;60"), ("30/60/90", 0, "30;60;90"), ("30/60/90/120", 0, "30;60;90;120"), ("50/80", 0, "50;80"),
        ("1+1", 50, "30"), ("1+2", 33.3333m, "30;60"), ("1+3", 25, "30;60;90"),
    ];

    public static async Task AplicarAsync(ISigeoDb db, Guid empresaId, CancellationToken ct = default)
    {
        var contas = await db.ContasGerenciais.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).Select(c => c.Codigo).ToListAsync(ct);
        foreach (var (cod, nome, nat) in PlanoContas.Where(c => !contas.Contains(c.Codigo)))
            db.ContasGerenciais.Add(new ContaGerencial { EmpresaId = empresaId, Codigo = cod, Nome = nome, Natureza = nat });

        var conds = await db.CondicoesPagamento.IgnoreQueryFilters().Where(c => c.EmpresaId == empresaId).Select(c => c.Nome).ToListAsync(ct);
        foreach (var (nome, ent, prazos) in Condicoes.Where(c => !conds.Contains(c.Nome)))
            db.CondicoesPagamento.Add(new CondicaoPagamento { EmpresaId = empresaId, Nome = nome, EntradaPerc = ent, Prazos = prazos });

        // RET (Lei 10.931/2004): 4% geral; 1% para interesse social (MCMV Faixa Urbano 1). Conferir com o contador; a reforma entra como nova vigência.
        var aliqs = await db.AliquotasRegime.IgnoreQueryFilters().Where(a => a.EmpresaId == empresaId).Select(a => a.Regime).ToListAsync(ct);
        if (!aliqs.Contains(RegimeTributario.RetGeral))
            db.AliquotasRegime.Add(new AliquotaRegime
            {
                EmpresaId = empresaId, Regime = RegimeTributario.RetGeral, VigenciaInicio = new DateOnly(2013, 1, 1),
                Irpj = 1.26m, Csll = 0.66m, Pis = 0.37m, Cofins = 1.71m, CodigoReceita = "4095", Observacao = "RET 4% — conferir código e divisão com o contador."
            });
        if (!aliqs.Contains(RegimeTributario.RetSocial))
            db.AliquotasRegime.Add(new AliquotaRegime
            {
                EmpresaId = empresaId, Regime = RegimeTributario.RetSocial, VigenciaInicio = new DateOnly(2023, 1, 1),
                Irpj = 0.31m, Csll = 0.16m, Pis = 0.09m, Cofins = 0.44m, Observacao = "RET social 1% — informar o código de receita."
            });
    }
}
