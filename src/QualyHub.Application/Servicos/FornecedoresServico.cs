using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Regras;

namespace QualyHub.Application.Servicos;

/// <summary>Painel do fornecedor (ficha única): documentos, mensais, qualificação, desempenho e uso.</summary>
public sealed record PainelFornecedor(int DocsValidos, int DocsPendentes, int DocsVencidos, int DocsEmValidacao, IReadOnlyList<string> MensaisPendentes,
    IReadOnlyList<string> Bloqueios, int Qualificacoes, DateOnly? QualificacaoValidaAte, int Fvm, decimal? FvmAprovadasPerc, decimal? NotaMedia,
    int Pedidos, decimal ValorPedidos, int Contratos, decimal ValorContratos, int PlanosAbertos);

/// <summary>Cadastro único do fornecedor: dados, contatos, grupos do catálogo, serviços (ITS), documentos exigidos (pré-cadastro) e painel.</summary>
public sealed class FornecedoresServico(IFabricaDb fabrica, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    public async Task SalvarAsync(Fornecedor f, CancellationToken ct = default)
    {
        var e = new List<string>();
        var doc = new string((f.CpfCnpj ?? "").Where(char.IsDigit).ToArray());
        if (doc.Length == 14 && !Conversor.CnpjValido(doc)) e.Add("CNPJ inválido.");
        if (doc.Length == 11 && !Conversor.CpfValido(doc)) e.Add("CPF inválido.");
        if (doc.Length is not (0 or 11 or 14)) e.Add("CPF/CNPJ incompleto.");
        if (f.PreCadastro && doc.Length == 0) e.Add("Pré-cadastro: informe o CNPJ.");
        if (string.IsNullOrWhiteSpace(f.RazaoSocial)) e.Add("Informe a razão social.");
        if (f.TipoFornecimento == TipoFornecimento.Nenhum) e.Add("Informe o tipo de fornecimento (material e/ou serviço).");
        if (string.IsNullOrWhiteSpace(f.Emails)) e.Add("Informe ao menos um e-mail.");
        if (f.Contatos.Any(c => string.IsNullOrWhiteSpace(c.Nome))) e.Add("Contato sem nome.");
        if (f.CapitalSocial is < 0) e.Add("Capital social negativo.");
        if (f.Grupos.GroupBy(g => g.CategoriaProdutoId).Any(g => g.Count() > 1)) e.Add("Grupo do catálogo repetido.");
        ValidacaoException.Se(e);
        if (doc.Length == 14) f.CpfCnpj = Conversor.FormatarCnpj(doc);
        foreach (var c in f.Contatos) c.FornecedorId = f.Id;
        foreach (var g in f.Grupos) g.FornecedorId = f.Id;
        foreach (var s in f.Servicos) s.FornecedorId = f.Id;
        foreach (var x in f.Exigencias) x.FornecedorId = f.Id;
        await using var db = fabrica.Criar();
        if (doc.Length > 0 && await db.Fornecedores.AnyAsync(x => x.Id != f.Id && x.CpfCnpj == f.CpfCnpj, ct)) throw new RegraDeNegocioException("Já existe fornecedor com este CPF/CNPJ.");
        await Grafo.SalvarAsync(db, f, nameof(Fornecedor.Contatos), nameof(Fornecedor.Grupos), nameof(Fornecedor.Servicos), nameof(Fornecedor.Exigencias));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Documentos que a matriz pede para o tipo de fornecimento (com/sem funcionários) — base da seleção no pré-cadastro.</summary>
    public async Task<List<TipoDocumentoExigido>> MatrizAsync(TipoFornecimento tipo, bool incluirFuncionarios, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor && t.Ativo).OrderBy(t => t.Categoria).ThenBy(t => t.Ordem).ThenBy(t => t.Nome).ToListAsync(ct);
        return tipos.Where(t => MatrizExigencias.Aplica(t, tipo, incluirFuncionarios)).ToList();
    }

    public async Task SalvarTipoDocumentoAsync(TipoDocumentoExigido t, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(t.Nome)) e.Add("Informe o nome do documento.");
        if (t.Frequencia == FrequenciaDocumento.Mensal && t.DiaLimite is not (null or >= 1 and <= 28)) e.Add("Dia limite entre 1 e 28.");
        if (t.ValidadePadraoDias is < 0) e.Add("Validade não pode ser negativa.");
        ValidacaoException.Se(e);
        if (t.Frequencia == FrequenciaDocumento.Mensal) t.DiaLimite ??= 20;
        t.PorEntrega = t.Frequencia == FrequenciaDocumento.PorEntrega;
        t.Momento = t.Frequencia switch { FrequenciaDocumento.PorEntrega => MomentoDocumento.PorEntrega, FrequenciaDocumento.PorContrato => MomentoDocumento.PorContrato, _ => MomentoDocumento.Cadastro };
        await using var db = fabrica.Criar();
        await Permissoes.ExigirAsync(db, fabrica.Usuario, "alterar a matriz de documentos", Domain.Common.PerfilUsuario.Qualidade, Domain.Common.PerfilUsuario.Suprimentos);
        if (await db.TiposDocumento.AnyAsync(x => x.Id != t.Id && x.AplicaA == t.AplicaA && x.Nome == t.Nome, ct)) throw new RegraDeNegocioException("Já existe documento com este nome.");
        await Grafo.SalvarAsync(db, t);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PainelFornecedor> PainelAsync(Guid fornecedorId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var f = await db.Fornecedores.AsNoTracking().Include(x => x.Documentos).ThenInclude(d => d.TipoDocumento).Include(x => x.Qualificacoes).Include(x => x.Exigencias)
                        .FirstAsync(x => x.Id == fornecedorId, ct);
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor && t.Ativo).ToListAsync(ct);
        var exig = f.Exigencias.Count > 0 ? f.Exigencias.Select(x => x.TipoDocumentoExigidoId).ToHashSet() : tipos.Where(t => MatrizExigencias.Aplica(t, f.TipoFornecimento, f.IncluirDocsFuncionarios)).Select(t => t.Id).ToHashSet();
        var ultimos = tipos.Where(t => exig.Contains(t.Id) && !t.PorEntrega && t.Frequencia != FrequenciaDocumento.Mensal)
            .Select(t => f.Documentos.Where(d => !d.Excluido && d.TipoDocumentoExigidoId == t.Id).OrderByDescending(d => d.DataValidade ?? DateOnly.MaxValue).ThenByDescending(d => d.CriadoEm).FirstOrDefault())
            .ToList();
        var st = ultimos.Select(d => d is null ? StatusDocumento.PendenteEnvio : StatusDocumentoCalculo.Calcular(d, Hoje)).ToList();
        var mensais = TravasQualidade.DocumentosMensaisPendentes(f, tipos, Hoje).ToList();
        var bloq = TravasQualidade.VerificarFornecedor(f, Hoje).Bloqueios;
        var ini = Hoje.AddMonths(-6);
        var recs = await db.Recebimentos.AsNoTracking().Where(r => r.FornecedorId == f.Id && r.DataRecebimento >= new DateTimeOffset(ini.ToDateTime(TimeOnly.MinValue))).ToListAsync(ct);
        var avaliados = recs.Where(r => r.FvmAprovada is not null).ToList();
        var notas = await db.Medicoes.AsNoTracking().Where(m => m.Contrato.FornecedorId == f.Id && m.NotaFinal != null && m.PeriodoFim >= ini).Select(m => m.NotaFinal!.Value).ToListAsync(ct);
        // Totais calculados no banco (não carrega todos os pedidos/contratos com itens).
        var pedidosQ = db.PedidoItens.AsNoTracking().Where(i => db.Pedidos.Any(p => p.Id == i.PedidoId && p.FornecedorId == f.Id && p.Status != Domain.Common.StatusPedido.Cancelado));
        var pedidosN = await db.Pedidos.CountAsync(p => p.FornecedorId == f.Id && p.Status != Domain.Common.StatusPedido.Cancelado, ct);
        var pedidosV = (await pedidosQ.SumAsync(i => (decimal?)i.ValorTotal, ct) ?? 0)
                       + (await db.Pedidos.Where(p => p.FornecedorId == f.Id && p.Status != Domain.Common.StatusPedido.Cancelado).SumAsync(p => p.ValorFrete, ct) ?? 0);
        var contratosN = await db.Contratos.CountAsync(c => c.FornecedorId == f.Id, ct);
        var contratosV = await db.ContratoItens.AsNoTracking().Where(i => db.Contratos.Any(c => c.Id == i.ContratoId && c.FornecedorId == f.Id))
                                 .SumAsync(i => (decimal?)(i.Quantidade * i.PrecoUnitario), ct) ?? 0;
        var planos = await db.PlanosAcao.AsNoTracking().CountAsync(p => p.FornecedorId == f.Id && p.Status != StatusPlanoAcao.Concluida, ct);
        var vig = f.QualificacaoVigente(Hoje);
        return new PainelFornecedor(st.Count(s => s is StatusDocumento.Valido or StatusDocumento.AVencer), st.Count(s => s == StatusDocumento.PendenteEnvio),
            st.Count(s => s is StatusDocumento.Vencido or StatusDocumento.Reprovado), st.Count(s => s == StatusDocumento.EmValidacao), mensais, bloq,
            f.Qualificacoes.Count(q => !q.Excluido), vig?.DataValidade, avaliados.Count,
            avaliados.Count == 0 ? null : Math.Round(100m * avaliados.Count(r => r.FvmAprovada == true) / avaliados.Count, 1),
            notas.Count == 0 ? null : Math.Round(notas.Average(), 2), pedidosN, Math.Round(pedidosV, 2), contratosN, Math.Round(contratosV, 2), planos);
    }

    /// <summary>Fornecedores que atendem os grupos (e subgrupos) do catálogo ou as ITS — sugestão de convite da cotação.</summary>
    public async Task<HashSet<Guid>> QueAtendemAsync(IReadOnlyCollection<(Guid Grupo, Guid? Subgrupo)> grupos, IReadOnlyCollection<Guid> servicos, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var gIds = grupos.Select(g => g.Grupo).Distinct().ToList();
        // Filhos não têm filtro global: entra pelo fornecedor (empresa, não excluído, ativo).
        var fg = await db.FornecedorGrupos.AsNoTracking().Where(x => gIds.Contains(x.CategoriaProdutoId) && db.Fornecedores.Any(f => f.Id == x.FornecedorId && f.Ativo)).ToListAsync(ct);
        var r = fg.Where(x => grupos.Any(g => g.Grupo == x.CategoriaProdutoId && (x.Todos || g.Subgrupo is null || (x.Subgrupos ?? "").Contains(g.Subgrupo.Value.ToString()))))
                  .Select(x => x.FornecedorId).ToHashSet();
        if (servicos.Count > 0)
            foreach (var id in await db.FornecedorServicos.AsNoTracking().Where(x => servicos.Contains(x.ServicoControladoId) && db.Fornecedores.Any(f => f.Id == x.FornecedorId && f.Ativo))
                                       .Select(x => x.FornecedorId).ToListAsync(ct)) r.Add(id);
        return r;
    }
}
