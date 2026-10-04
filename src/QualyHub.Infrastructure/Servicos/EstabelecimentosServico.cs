using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Estrutura;
using QualyHub.Infrastructure.Dados;
using QualyHub.Infrastructure.Importacao;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Estabelecimentos (CNPJs), fluxos entre eles e plano único de centros de custo em árvore.</summary>
public sealed class EstabelecimentosServico(FabricaDb fabrica)
{
    public async Task SalvarAsync(Estabelecimento x, CancellationToken ct = default)
    {
        var e = new List<string>();
        var doc = Conversor.SomenteDigitos(x.Cnpj);
        if (!Conversor.CnpjValido(doc)) e.Add("CNPJ inválido.");
        if (string.IsNullOrWhiteSpace(x.RazaoSocial)) e.Add("Informe a razão social.");
        x.Codigo = (x.Codigo ?? "").Trim().ToUpperInvariant();
        if (x.Codigo.Length is < 2 or > 20) e.Add("Informe o código (ex.: FAB-PM).");
        ValidacaoException.Se(e);
        x.Cnpj = Convert.ToUInt64(doc).ToString(@"00\.000\.000\/0000\-00");
        x.Raiz = doc[..8];
        await using var db = fabrica.Criar();
        if (await db.Estabelecimentos.AnyAsync(o => o.Id != x.Id && o.Cnpj == x.Cnpj, ct)) throw new RegraDeNegocioException("Já existe estabelecimento com este CNPJ.");
        if (await db.Estabelecimentos.AnyAsync(o => o.Id != x.Id && o.Codigo == x.Codigo, ct)) throw new RegraDeNegocioException("Código já usado.");
        // Só um padrão de faturamento de materiais e um almoxarifado central.
        if (x.FaturaMateriais) await db.Estabelecimentos.Where(o => o.Id != x.Id && o.FaturaMateriais).ForEachAsync(o => o.FaturaMateriais = false, ct);
        if (x.AlmoxarifadoCentral) await db.Estabelecimentos.Where(o => o.Id != x.Id && o.AlmoxarifadoCentral).ForEachAsync(o => o.AlmoxarifadoCentral = false, ct);
        await Grafo.SalvarAsync(db, x);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Cada SPE vira (ou atualiza) um estabelecimento do tipo SPE — o "Faturar para" e o mapa enxergam todos os CNPJs.</summary>
    public static async Task SincronizarSpeAsync(QualyHubDbContext db, Spe s, CancellationToken ct)
    {
        var x = await db.Estabelecimentos.FirstOrDefaultAsync(e => e.SpeId == s.Id, ct)
                ?? await db.Estabelecimentos.FirstOrDefaultAsync(e => e.Cnpj == s.Cnpj, ct);
        if (x is null) { x = new Estabelecimento { Tipo = TipoEstabelecimento.Spe, Codigo = $"SPE-{s.Codigo}" }; db.Estabelecimentos.Add(x); }
        if (await db.Estabelecimentos.AnyAsync(o => o.Id != x.Id && o.Codigo == x.Codigo, ct)) x.Codigo = $"SPE-{s.Codigo}-{s.Cnpj[^2..]}";
        x.SpeId = s.Id; x.Tipo = TipoEstabelecimento.Spe; x.RazaoSocial = s.RazaoSocial; x.NomeFantasia = s.NomeFantasia; x.Cnpj = s.Cnpj;
        x.Raiz = Conversor.SomenteDigitos(s.Cnpj)[..8]; x.InscricaoEstadual = s.InscricaoEstadual; x.Endereco = s.EnderecoFaturamento; x.Ativo = s.Ativa;
    }

    /// <summary>Estabelecimento padrão do "Faturar para" nas compras de material.</summary>
    public static Task<Guid?> PadraoFaturamentoAsync(QualyHubDbContext db, CancellationToken ct = default) =>
        db.Estabelecimentos.Where(e => e.FaturaMateriais && e.Ativo).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);

    public async Task SalvarFluxoAsync(FluxoEstabelecimento f, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (f.OrigemId == Guid.Empty || f.DestinoId == Guid.Empty) e.Add("Informe origem e destino.");
        if (f.OrigemId == f.DestinoId) e.Add("Origem e destino devem ser diferentes.");
        if (string.IsNullOrWhiteSpace(f.Base)) e.Add("Informe a base do valor.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, f);
        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirFluxoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var f = await db.FluxosEstabelecimento.FirstAsync(x => x.Id == id, ct);
        db.Remove(f);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarCentroCustoAsync(CentroCusto c, CancellationToken ct = default)
    {
        var e = new List<string>();
        c.Codigo = (c.Codigo ?? "").Trim();
        if (c.Codigo.Length == 0) e.Add("Informe o código (ex.: 3.1).");
        if (string.IsNullOrWhiteSpace(c.Nome)) e.Add("Informe o nome.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (await db.CentrosCusto.AnyAsync(x => x.Id != c.Id && x.Codigo == c.Codigo, ct)) throw new RegraDeNegocioException("Código já usado.");
        if (c.PaiId is { } pai)
        {
            var todos = await db.CentrosCusto.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.PaiId, ct);
            Guid? p = pai;
            while (p is { } id) { if (id == c.Id) throw new RegraDeNegocioException("O centro de custo não pode ficar abaixo dele mesmo."); p = todos.GetValueOrDefault(id); }
        }
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
    }
}
