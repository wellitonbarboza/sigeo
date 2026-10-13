using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Domain.Qualidade;

namespace QualyHub.Application.Servicos;

/// <summary>Contrato de serviço (T11/T12) e aditivos (T28).</summary>
public sealed class ContratosServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T11/T12 Contrato ─────────

    public async Task<Guid> SalvarContratoAsync(Contrato k, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (string.IsNullOrWhiteSpace(k.Numero))
            k.Numero = $"CT-{Hoje.Year}-{await Numerador.ProximoAsync(db, $"Contrato-{Hoje.Year}", ct):000}";
        var statusAtual = await db.Contratos.Where(x => x.Id == k.Id).Select(x => (StatusContrato?)x.Status).FirstOrDefaultAsync(ct) ?? k.Status;
        if (statusAtual is not StatusContrato.EmElaboracao) throw new RegraDeNegocioException("Contrato fora de elaboração: use aditivo.");
        // Obra própria (sem SPE): o contrato fica no CNPJ da construtora dono da obra.
        if (k.EstabelecimentoFaturamentoId is null && k.SpeId is null)
            k.EstabelecimentoFaturamentoId = await db.Obras.Where(o => o.Id == k.ObraId && o.DonoCusto == DonoCustoObra.Construtora).Select(o => o.EstabelecimentoDonoId).FirstOrDefaultAsync(ct);
        // Quebra por local (netos): se a quantidade do item mudou e os locais ainda somam a quantidade antiga, reescalona proporcionalmente.
        var qtdAntiga = await db.ContratoItens.Where(i => i.ContratoId == k.Id).ToDictionaryAsync(i => i.Id, i => i.Quantidade, ct);
        var locais = new Dictionary<Guid, List<ContratoItemLocal>>();
        foreach (var i in k.Itens)
        {
            var ls = i.Locais.GroupBy(l => l.LocalId).Select(g => new ContratoItemLocal { ContratoItemId = i.Id, LocalId = g.Key, Quantidade = g.Sum(l => l.Quantidade) }).ToList();
            if (ls.Count > 0 && qtdAntiga.TryGetValue(i.Id, out var antes) && antes != i.Quantidade && Math.Abs(ls.Sum(l => l.Quantidade) - antes) < 0.001m)
            {
                var novas = Rateio.Proporcional(i.Quantidade * 100m, ls.Select(l => l.Quantidade).ToList()); // ×100: rateio com 4 casas
                for (var n = 0; n < ls.Count; n++) ls[n].Quantidade = novas[n] / 100m;
            }
            locais[i.Id] = ls;
            i.Locais = [];
        }
        var errosLocais = k.Itens.Where(i => locais[i.Id].Sum(l => l.Quantidade) > i.Quantidade + 0.0001m)
                                 .Select(i => $"Item {i.Item}: soma dos locais maior que a quantidade do item.").ToList();
        ValidacaoException.Se(errosLocais);
        var obraLocais = locais.Values.SelectMany(x => x).Select(l => l.LocalId).Distinct().ToList();
        if (obraLocais.Count > 0 && await db.Locais.CountAsync(l => obraLocais.Contains(l.Id) && l.ObraId == k.ObraId, ct) != obraLocais.Count)
            throw new RegraDeNegocioException("Há locais que não pertencem à obra do contrato.");
        db.ContratoItemLocais.RemoveRange(await db.ContratoItemLocais.Where(x => db.ContratoItens.Any(i => i.Id == x.ContratoItemId && i.ContratoId == k.Id)).ToListAsync(ct));
        await Grafo.SalvarAsync(db, k, nameof(Contrato.Itens), nameof(Contrato.ServicosVinculados));
        db.ContratoItemLocais.AddRange(locais.Values.SelectMany(x => x));
        await db.SaveChangesAsync(ct);
        foreach (var i in k.Itens) i.Locais = locais[i.Id];
        return k.Id;
    }

    public async Task EnviarContratoAprovacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).Include(x => x.ServicosVinculados)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Qualificacoes)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .FirstAsync(x => x.Id == id, ct);
        ValidacaoException.Se(TravasQualidade.VerificarContrato(k, Hoje).Bloqueios);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Contrato, k.Id, k.ObraId, $"{k.Numero} – {k.Fornecedor.NomeExibicao}", k.ValorTotal, ct, k.SpeId);
        k.Status = StatusContrato.AguardandoAprovacaoDiretoria;
        await db.SaveChangesAsync(ct);
    }

    public async Task AtivarContratoAsync(Guid id, DateOnly assinaturaEmpresa, DateOnly assinaturaFornecedor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.FirstAsync(x => x.Id == id, ct);
        k.AssinaturaEmpresa = assinaturaEmpresa;
        k.AssinaturaFornecedor = assinaturaFornecedor;
        k.Ativar();
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEnvioContratoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.FirstAsync(x => x.Id == id, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Ative o contrato (assinaturas) antes de enviar ao fornecedor.");
        k.EnviadoFornecedorEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    // ───────── T28 Aditivo ─────────

    public async Task<Aditivo> NovoAditivoAsync(Guid contratoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.AsNoTracking().FirstAsync(x => x.Id == contratoId, ct);
        if (k.Status != StatusContrato.Ativo) throw new RegraDeNegocioException("Aditivo só em contrato ativo.");
        if (await db.Aditivos.AnyAsync(a => a.ContratoId == contratoId && (a.Status == StatusAditivo.EmElaboracao || a.Status == StatusAditivo.AguardandoAprovacao || a.Status == StatusAditivo.AguardandoAssinaturas), ct))
            throw new RegraDeNegocioException("Já existe aditivo em andamento neste contrato.");
        var n = await db.Aditivos.CountAsync(a => a.ContratoId == contratoId, ct) + 1;
        return new Aditivo { ContratoId = k.Id, ObraId = k.ObraId, Numero = $"AD-{n:00}" };
    }

    public async Task<Guid> SalvarAditivoAsync(Aditivo a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var k = await db.Contratos.Include(x => x.Itens).AsNoTracking().FirstAsync(x => x.Id == a.ContratoId, ct);
        if (a.Status != StatusAditivo.EmElaboracao) throw new RegraDeNegocioException("Aditivo fora de elaboração não pode ser alterado.");
        foreach (var i in a.Itens)
        {
            i.AditivoId = a.Id;
            if (k.Itens.FirstOrDefault(x => x.Id == i.ContratoItemId) is { } ci) { i.QtdOriginal = ci.Quantidade; i.PrecoOriginal = ci.PrecoUnitario; }
        }
        ValidacaoException.Se(a.Validar(k));
        var original = await OriginalAsync(db, k, ct);
        var anteriores = await db.Aditivos.Where(x => x.ContratoId == k.Id && x.Status == StatusAditivo.Vigente && x.Id != a.Id).SumAsync(x => x.Valor, ct);
        a.Calcular(original, anteriores);
        a.Contrato = null;
        await Grafo.SalvarAsync(db, a, nameof(Aditivo.Itens));
        await db.SaveChangesAsync(ct);
        return a.Id;
    }

    /// <summary>Valor original = valor atual − aditivos vigentes (os itens do contrato já refletem os aditivos aplicados).</summary>
    private static async Task<decimal> OriginalAsync(IQualyHubDb db, Contrato k, CancellationToken ct) =>
        k.ValorTotal - await db.Aditivos.Where(x => x.ContratoId == k.Id && x.Status == StatusAditivo.Vigente).SumAsync(x => x.Valor, ct);

    public async Task EnviarAditivoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.Aditivos.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        var k = await db.Contratos.Include(x => x.Fornecedor).FirstAsync(x => x.Id == a.ContratoId, ct);
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Aditivo, a.Id, a.ObraId, $"{k.Numero} · {a.Numero} – {k.Fornecedor.NomeExibicao}", Math.Abs(a.Valor), ct, k.SpeId);
        a.Status = StatusAditivo.AguardandoAprovacao;
        await db.SaveChangesAsync(ct);
    }

    public async Task AssinarAditivoAsync(Guid id, DateOnly empresa, DateOnly fornecedor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.Aditivos.Include(x => x.Itens).FirstAsync(x => x.Id == id, ct);
        if (a.Status != StatusAditivo.AguardandoAssinaturas) throw new RegraDeNegocioException("Aditivo não está aguardando assinaturas.");
        var k = await db.Contratos.Include(x => x.Itens).FirstAsync(x => x.Id == a.ContratoId, ct);
        a.AssinaturaEmpresa = empresa; a.AssinaturaFornecedor = fornecedor;
        a.Aplicar(k);
        await db.SaveChangesAsync(ct);
    }
}
