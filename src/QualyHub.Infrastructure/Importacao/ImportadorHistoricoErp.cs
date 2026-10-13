using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QualyHub.Application.Abstracoes;
using QualyHub.Application.Comum;
using QualyHub.Application.Servicos;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Engenharia;
using QualyHub.Domain.Importacao;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Infrastructure.Importacao;

public sealed record ArquivoRelatorioErp(string Nome, byte[] Conteudo);
public sealed record ArquivoLido(string Nome, string Relatorio, int Linhas, bool Usado, string? Observacao);
public sealed record SugestaoDestino(Guid? Id, string Texto, decimal Nota);

/// <summary>Um texto de origem (insumo, fornecedor, fase, serviço/local) e o que vai acontecer com ele na carga.</summary>
public sealed record ItemDePara(TipoVinculoOrigem Tipo, string Chave, string Texto, string? Info, int Linhas, decimal Valor,
    AcaoDePara Acao, Guid? DestinoId, string? DestinoTexto, IReadOnlyList<SugestaoDestino> Sugestoes, bool Salvo);

/// <summary>Vincular = usa o cadastro escolhido/sugerido; Criar = cadastra novo; Ignorar = não liga a cadastro (o custo entra só com o texto).</summary>
public enum AcaoDePara { Vincular = 1, Criar = 2, Ignorar = 3 }

public sealed record ResumoHistoricoErp(Guid LoteId, Guid ObraId, string Obra, DateOnly? Corte, StatusImportacao Status,
    IReadOnlyList<ArquivoLido> Arquivos, DateOnly? De, DateOnly? Ate,
    int LinhasCusto, decimal ValorCusto, int LinhasAposCorte, decimal ValorAposCorte, int CustosJaImportados,
    int Pedidos, int ItensPedido, decimal ValorPedidos, int PedidosJaImportados, int PedidosEmAberto,
    IReadOnlyList<ItemDePara> Produtos, IReadOnlyList<ItemDePara> Fornecedores, IReadOnlyList<ItemDePara> Fases, IReadOnlyList<ItemDePara> Locais,
    IReadOnlyList<string> Avisos);

public sealed record ResultadoCargaErp(int Custos, decimal ValorCustos, int Pedidos, int ItensPedido, int ProdutosCriados, int FornecedoresCriados, int Ignorados);

/// <summary>
/// Importação do histórico de uma obra a partir dos relatórios do ERP (apropriações de insumos, medição de ordens de compra,
/// fornecedores e insumos cadastrados). Em duas etapas: o envio monta a simulação (nada entra nos cadastros); o de-para é revisado
/// na tela e gravado (vale para as próximas cargas); a confirmação grava pedidos históricos e custo realizado antes da data de corte.
/// Idempotente: reenviar os mesmos relatórios não duplica (chave de origem por linha).
/// </summary>
public sealed class ImportadorHistoricoErp(IFabricaDb fabrica, TimeProvider relogio)
{
    public const string Sistema = "ERP";
    private const string KApr = "apropriacao", KOc = "oc", KForn = "fornecedor", KIns = "insumo", KArq = "arquivos";
    private static readonly System.Globalization.CultureInfo Br = new("pt-BR");
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    // ───────────── Registros guardados no lote (DadosJson) ─────────────
    private sealed record RegApr(DateOnly Data, string? Oc, string? Doc, string? Nf, string? Fornecedor, string? TipoCusto, string? Categoria,
        string Insumo, decimal Qtd, decimal Valor, string? Fase, string? Servico);
    private sealed record RegOc(string Oc, string? Fornecedor, DateOnly? Criacao, DateOnly? Previsao, string? Condicao, string Item,
        decimal Qtd, decimal ValorUnitario, decimal Total, decimal QtdRecebida, DateOnly? DataRecebimento);
    private sealed record RegForn(string RazaoSocial, string? Fantasia, string? Documento, string? CidadeUf, string? Telefone, string? Email);
    private sealed record RegIns(string? Codigo, string Nome, string? Categoria, string? TipoCusto, string? Unidade, decimal? Preco);

    // ───────────── 1. Envio: lê os relatórios e cria o lote ─────────────

    public async Task<Guid> EnviarAsync(Guid obraId, IReadOnlyList<ArquivoRelatorioErp> arquivos, CancellationToken ct = default)
    {
        if (arquivos.Count == 0) throw new RegraDeNegocioException("Envie ao menos o relatório de apropriações de insumos.");
        var lidos = new List<ArquivoLido>();
        var linhas = new List<LinhaImportacao>();
        var n = 0;
        foreach (var a in arquivos)
        {
            using var ms = new MemoryStream(a.Conteudo);
            var r = LeitorRelatorioErp.Ler(ms, a.Nome);
            var antes = linhas.Count;
            switch (r.Tipo)
            {
                case TipoRelatorioErp.Apropriacoes:
                    foreach (var l in r.Linhas)
                    {
                        var insumo = l.Texto("insumo"); var valor = l.Dec("total(r$)"); var data = l.Data("entrada");
                        if (insumo is null || valor is null || data is null) continue;
                        Add(KApr, new RegApr(data.Value, SemZeros(l.Texto("o.c.")), l.Texto("doc."), l.Texto("numero"), l.Texto("fornecedor"), l.Texto("tipocusto"),
                            l.Texto("categoria"), insumo, l.Dec("qtd.") ?? l.Dec("qtd.total") ?? 0, valor.Value, l.Texto("fase"), l.Texto("servico")));
                    }
                    break;
                case TipoRelatorioErp.MedicaoOrdensCompra:
                    foreach (var l in r.Linhas)
                    {
                        var oc = SemZeros(l.Texto("ordemcompra")); var item = l.Texto("item");
                        if (oc is null || item is null) continue;
                        var qtd = l.Dec("qtd.") ?? 0; var vu = l.Dec("vlr.unitario(r$)") ?? 0;
                        Add(KOc, new RegOc(oc, l.Texto("fornecedor"), l.Data("criacao"), l.Data("previsaoentrega"), l.Texto("condicaopgto."), item,
                            qtd, vu, l.Dec("total(r$)") ?? Math.Round(qtd * vu, 2), l.Dec("totalreceb.") ?? 0, l.Data("dt.receb.")));
                    }
                    break;
                case TipoRelatorioErp.Fornecedores:
                    foreach (var l in r.Linhas)
                        if (l.Texto("razaosocial") is { } rs)
                            Add(KForn, new RegForn(rs, l.Texto("nomefantasia"), l.Texto("cpf/cnpj"), l.Texto("cidade/uf"), l.Texto("telefone"), l.Texto("e-mail")));
                    break;
                case TipoRelatorioErp.Insumos:
                    foreach (var l in r.Linhas)
                        if (l.Texto("nome") is { } nome)
                            Add(KIns, new RegIns(l.Texto("codigo"), nome, l.Texto("categoria"), l.Texto("tipodecusto"), l.Texto("unidade"), l.Dec("vlr.unitario(r$)")));
                    break;
            }
            var usados = linhas.Count - antes;
            lidos.Add(new ArquivoLido(a.Nome, LeitorRelatorioErp.Rotulo(r.Tipo), usados, usados > 0, r.Tipo switch
            {
                TipoRelatorioErp.DocumentosFinanceiros => "Contas a pagar entram com o módulo Financeiro (o custo já vem das apropriações).",
                TipoRelatorioErp.Desconhecido => "Relatório não reconhecido — use apropriações, medição de OCs, fornecedores ou insumos.",
                _ => null
            }));
        }
        if (!linhas.Any(l => l.ChaveNatural == KApr) && !linhas.Any(l => l.ChaveNatural == KOc))
            throw new RegraDeNegocioException("Nenhum relatório de apropriações de insumos ou de medição de ordens de compra foi reconhecido nos arquivos.");

        await using var db = fabrica.Criar();
        if (!await db.Obras.AnyAsync(o => o.Id == obraId, ct)) throw new RegraDeNegocioException("Obra não encontrada.");
        var lote = new LoteImportacao
        {
            Tipo = TipoImportacao.HistoricoObraErp, ObraId = obraId,
            ArquivoNomeOriginal = Cortar(string.Join("; ", arquivos.Select(a => a.Nome)), 250)!,
            ArquivoBlob = "", ArquivoHashSha256 = Convert.ToHexString(SHA256.HashData(arquivos.SelectMany(a => a.Conteudo).ToArray())),
            Status = StatusImportacao.AguardandoConfirmacao, TotalLinhas = linhas.Count
        };
        linhas.Add(new LinhaImportacao { NumeroLinha = 0, ChaveNatural = KArq, DadosJson = JsonSerializer.Serialize(lidos, Json), Acao = AcaoLinhaImportacao.SemAlteracao });
        foreach (var l in linhas) l.LoteImportacaoId = lote.Id;
        db.LotesImportacao.Add(lote);
        db.Set<LinhaImportacao>().AddRange(linhas);
        await db.SaveChangesAsync(ct);
        return lote.Id;

        void Add<T>(string tipo, T reg) => linhas.Add(new LinhaImportacao
        {
            NumeroLinha = ++n, ChaveNatural = tipo, DadosJson = JsonSerializer.Serialize(reg, Json), Acao = AcaoLinhaImportacao.Inserir
        });
    }

    public async Task<List<LoteImportacao>> LotesAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.LotesImportacao.Where(l => l.ObraId == obraId && l.Tipo == TipoImportacao.HistoricoObraErp)
            .OrderByDescending(l => l.CriadoEm).Take(20).ToListAsync(ct);
    }

    // ───────────── 2. Simulação (resumo + de-para) ─────────────

    private sealed class Contexto
    {
        public required LoteImportacao Lote;
        public required Domain.Estrutura.Obra Obra;
        public List<RegApr> Apr = [];
        public List<RegOc> Oc = [];
        public List<RegForn> Forn = [];
        public List<RegIns> Ins = [];
        public List<ArquivoLido> Arquivos = [];
        public List<VinculoOrigem> Vinculos = [];
        public List<SugestoesImportacao.Candidato> Produtos = [];
        private SugestoesImportacao.IndiceProdutos? _indice;
        public SugestoesImportacao.IndiceProdutos Indice => _indice ??= new(Produtos);
        private Dictionary<string, RegIns>? _ins;
        public Dictionary<string, RegIns> InsPorChave => _ins ??= Ins.GroupBy(i => ChaveProduto(i.Nome)).ToDictionary(g => g.Key, g => g.First());
        private Dictionary<string, RegForn>? _forn;
        public Dictionary<string, RegForn> FornPorChave => _forn ??= Forn
            .SelectMany(f => new[] { (K: SugestoesImportacao.Chave(f.RazaoSocial), F: f), (K: SugestoesImportacao.Chave(f.Fantasia), F: f) })
            .Where(x => x.K.Length > 0).GroupBy(x => x.K).ToDictionary(g => g.Key, g => g.First().F);
        public List<(Guid Id, string Nome, string Chave, string Doc)> Fornecedores = [];
        public List<string> Fases = [];
        public List<SugestoesImportacao.LocalCandidato> Locais = [];
        public List<string> Grupos = [];
    }

    private async Task<Contexto> CarregarAsync(IQualyHubDb db, Guid loteId, CancellationToken ct)
    {
        var lote = await db.LotesImportacao.AsNoTracking().FirstOrDefaultAsync(l => l.Id == loteId && l.Tipo == TipoImportacao.HistoricoObraErp, ct)
                   ?? throw new RegraDeNegocioException("Importação não encontrada.");
        var obra = await db.Obras.AsNoTracking().FirstAsync(o => o.Id == lote.ObraId, ct);
        var cx = new Contexto { Lote = lote, Obra = obra };
        foreach (var l in await db.Set<LinhaImportacao>().AsNoTracking().Where(l => l.LoteImportacaoId == loteId).OrderBy(l => l.NumeroLinha).ToListAsync(ct))
            switch (l.ChaveNatural)
            {
                case KApr: cx.Apr.Add(JsonSerializer.Deserialize<RegApr>(l.DadosJson)!); break;
                case KOc: cx.Oc.Add(JsonSerializer.Deserialize<RegOc>(l.DadosJson)!); break;
                case KForn: cx.Forn.Add(JsonSerializer.Deserialize<RegForn>(l.DadosJson)!); break;
                case KIns: cx.Ins.Add(JsonSerializer.Deserialize<RegIns>(l.DadosJson)!); break;
                case KArq: cx.Arquivos = JsonSerializer.Deserialize<List<ArquivoLido>>(l.DadosJson) ?? []; break;
            }
        cx.Vinculos = await db.VinculosOrigem.AsNoTracking().Where(v => v.Sistema == Sistema && (v.ObraId == null || v.ObraId == obra.Id)).ToListAsync(ct);
        cx.Produtos = (await db.Produtos.AsNoTracking().Where(p => p.Ativo).Select(p => new { p.Id, p.Nome, p.Unidade }).ToListAsync(ct))
            .Select(p => new SugestoesImportacao.Candidato(p.Id, p.Nome, p.Unidade)).ToList();
        cx.Fornecedores = (await db.Fornecedores.AsNoTracking().Select(f => new { f.Id, f.RazaoSocial, f.NomeFantasia, f.CpfCnpj }).ToListAsync(ct))
            .SelectMany(f => new[] { (f.Id, f.RazaoSocial, SugestoesImportacao.Chave(f.RazaoSocial), Conversor.SomenteDigitos(f.CpfCnpj)) }
                .Concat(string.IsNullOrWhiteSpace(f.NomeFantasia) ? [] : [(f.Id, f.RazaoSocial, SugestoesImportacao.Chave(f.NomeFantasia), Conversor.SomenteDigitos(f.CpfCnpj))]))
            .ToList();
        cx.Fases = await db.Listas.AsNoTracking().Where(i => i.Tipo == TipoLista.GrupoServico && i.Ativo).OrderBy(i => i.Ordem).Select(i => i.Nome).ToListAsync(ct);
        if (!cx.Fases.Contains(CustoRealizado.FaseAApropriar)) cx.Fases.Add(CustoRealizado.FaseAApropriar);
        cx.Locais = (await db.Locais.AsNoTracking().Where(l => l.ObraId == obra.Id && l.Tipo != "Cômodo").Select(l => new { l.Id, l.Nome, l.Tipo, l.Ordem }).ToListAsync(ct))
            .Select(l => new SugestoesImportacao.LocalCandidato(l.Id, l.Nome, l.Tipo, l.Ordem)).ToList();
        cx.Grupos = await db.Produtos.AsNoTracking().Where(p => p.Grupo != null).Select(p => p.Grupo!).Distinct().ToListAsync(ct);
        return cx;
    }

    private static VinculoOrigem? Vinculo(Contexto cx, TipoVinculoOrigem tipo, string chave) =>
        cx.Vinculos.FirstOrDefault(v => v.Tipo == tipo && v.Chave == chave && (tipo != TipoVinculoOrigem.Local || v.ObraId == cx.Obra.Id));

    private static string ChaveProduto(string nome) => SugestoesImportacao.Chave(SugestoesImportacao.LimparNome(nome));

    private static RegIns? InsumoDe(Contexto cx, string chave) => cx.InsPorChave.GetValueOrDefault(chave);

    private static List<ItemDePara> DeParaProdutos(Contexto cx)
    {
        var usos = cx.Apr.Select(a => (Nome: a.Insumo, a.Valor, a.TipoCusto, a.Categoria))
            .Concat(cx.Oc.Select(o => (Nome: o.Item, Valor: o.Total, TipoCusto: (string?)null, Categoria: (string?)null)))
            .GroupBy(x => ChaveProduto(x.Nome)).Where(g => g.Key.Length > 0);
        var lista = new List<ItemDePara>();
        foreach (var g in usos)
        {
            var texto = SugestoesImportacao.LimparNome(g.First().Nome);
            var ins = InsumoDe(cx, g.Key);
            var un = UnidadesPadrao.Normalizar(ins?.Unidade);
            var tipo = SugestoesImportacao.TipoCustoDe(ins?.TipoCusto ?? g.FirstOrDefault(x => x.TipoCusto != null).TipoCusto);
            var cat = ins?.Categoria ?? g.FirstOrDefault(x => x.Categoria != null).Categoria;
            var info = $"{un} · {TiposCusto.Nome(tipo)}" + (cat is null ? "" : $" · {cat}");
            var sug = cx.Indice.Sugerir(texto, un).Select(s => new SugestaoDestino(s.Id, $"{s.Nome} ({s.Unidade})", s.Nota)).ToList();
            var v = Vinculo(cx, TipoVinculoOrigem.Produto, g.Key);
            var (acao, destino) = v is not null ? (v.Ignorar ? AcaoDePara.Ignorar : v.DestinoId is null ? AcaoDePara.Criar : AcaoDePara.Vincular, v.DestinoId)
                : sug.FirstOrDefault() is { Nota: >= SugestoesImportacao.NotaVinculoAutomatico } s ? (AcaoDePara.Vincular, s.Id) : (AcaoDePara.Criar, (Guid?)null);
            var destTexto = destino is { } d ? cx.Produtos.FirstOrDefault(p => p.Id == d) is { } p ? $"{p.Nome} ({p.Unidade})" : "(produto excluído)" : null;
            lista.Add(new ItemDePara(TipoVinculoOrigem.Produto, g.Key, texto, info, g.Count(), g.Sum(x => x.Valor), acao, destino, destTexto, sug, v is not null));
        }
        return lista.OrderBy(i => i.Acao == AcaoDePara.Vincular).ThenByDescending(i => i.Valor).ToList();
    }

    private static (Guid? Id, string? Nome) FornecedorExistente(Contexto cx, string chave, string? doc)
    {
        if (doc is { Length: 11 or 14 } && cx.Fornecedores.FirstOrDefault(f => f.Doc == doc) is { Id: var idDoc, Nome: var n1 } && idDoc != Guid.Empty) return (idDoc, n1);
        var porNome = cx.Fornecedores.FirstOrDefault(f => f.Chave == chave);
        return porNome.Id == Guid.Empty ? (null, null) : (porNome.Id, porNome.Nome);
    }

    private static string? DocumentoDe(Contexto cx, string chave)
    {
        var d = Conversor.SomenteDigitos(cx.FornPorChave.GetValueOrDefault(chave)?.Documento);
        return d.Length is 11 or 14 ? d : null;
    }

    private static List<ItemDePara> DeParaFornecedores(Contexto cx)
    {
        var usos = cx.Apr.Where(a => a.Fornecedor != null).Select(a => (Nome: a.Fornecedor!, a.Valor))
            .Concat(cx.Oc.Where(o => o.Fornecedor != null).Select(o => (Nome: o.Fornecedor!, Valor: o.Total)))
            .GroupBy(x => SugestoesImportacao.Chave(x.Nome)).Where(g => g.Key.Length > 0);
        var lista = new List<ItemDePara>();
        foreach (var g in usos)
        {
            var doc = DocumentoDe(cx, g.Key);
            var v = Vinculo(cx, TipoVinculoOrigem.Fornecedor, g.Key);
            var (idEx, nomeEx) = FornecedorExistente(cx, g.Key, doc);
            var (acao, destino) = v is not null ? (v.Ignorar ? AcaoDePara.Ignorar : v.DestinoId is null ? AcaoDePara.Criar : AcaoDePara.Vincular, v.DestinoId)
                : idEx is not null ? (AcaoDePara.Vincular, idEx) : (AcaoDePara.Criar, (Guid?)null);
            var destTexto = destino is { } d ? cx.Fornecedores.FirstOrDefault(f => f.Id == d).Nome ?? "(fornecedor excluído)" : null;
            lista.Add(new ItemDePara(TipoVinculoOrigem.Fornecedor, g.Key, g.First().Nome.Trim(), doc is null ? "sem CPF/CNPJ no relatório" : FormatarDoc(doc),
                g.Count(), g.Sum(x => x.Valor), acao, destino, destTexto,
                idEx is { } i && nomeEx is not null ? [new SugestaoDestino(i, nomeEx, 1)] : [], v is not null));
        }
        return lista.OrderBy(i => i.Acao == AcaoDePara.Vincular).ThenByDescending(i => i.Valor).ToList();
    }

    private static List<ItemDePara> DeParaFases(Contexto cx) =>
        cx.Apr.GroupBy(a => SugestoesImportacao.Chave(a.Fase)).Select(g =>
        {
            var v = Vinculo(cx, TipoVinculoOrigem.Fase, g.Key);
            var sug = SugestoesImportacao.SugerirFase(g.First().Fase, cx.Fases);
            var destino = v?.DestinoTexto ?? sug;
            return new ItemDePara(TipoVinculoOrigem.Fase, g.Key, g.First().Fase ?? "(sem fase)", null, g.Count(), g.Sum(x => x.Valor),
                AcaoDePara.Vincular, null, destino, [new SugestaoDestino(null, sug, 1)], v is not null);
        }).OrderByDescending(i => i.Valor).ToList();

    private static List<ItemDePara> DeParaLocais(Contexto cx) =>
        cx.Apr.GroupBy(a => SugestoesImportacao.Chave(a.Servico)).Select(g =>
        {
            var v = Vinculo(cx, TipoVinculoOrigem.Local, g.Key);
            var sug = SugestoesImportacao.SugerirLocal(g.First().Servico, cx.Locais);
            var destino = v is not null ? v.DestinoId : sug;
            var nome = destino is { } d ? cx.Locais.FirstOrDefault(l => l.Id == d)?.Nome : null;
            return new ItemDePara(TipoVinculoOrigem.Local, g.Key, g.First().Servico ?? "(sem serviço)", null, g.Count(), g.Sum(x => x.Valor),
                destino is null ? AcaoDePara.Ignorar : AcaoDePara.Vincular, destino, nome ?? "Obra toda (sem local)",
                sug is { } s ? [new SugestaoDestino(s, cx.Locais.First(l => l.Id == s).Nome, 1)] : [], v is not null);
        }).OrderByDescending(i => i.Valor).ToList();

    public async Task<ResumoHistoricoErp> SimularAsync(Guid loteId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var cx = await CarregarAsync(db, loteId, ct);
        var corte = cx.Obra.DataCorteSistema;
        var antes = cx.Apr.Where(a => corte is null || a.Data < corte).ToList();
        var depois = cx.Apr.Where(a => corte is not null && a.Data >= corte).ToList();
        var chaves = ChavesCusto(antes).Select(x => x.Chave).ToList();
        var jaCustos = chaves.Count == 0 ? 0 : await db.CustosRealizados.CountAsync(c => c.ObraId == cx.Obra.Id && chaves.Contains(c.ChaveOrigem!), ct);
        var ocs = cx.Oc.GroupBy(o => o.Oc).ToList();
        var codigos = ocs.Select(g => CodigoPedido(g.Key)).ToList();
        var jaPedidos = codigos.Count == 0 ? 0 : await db.Pedidos.CountAsync(p => p.ObraId == cx.Obra.Id && codigos.Contains(p.CodigoErp!), ct);
        var avisos = new List<string>();
        if (corte is null) avisos.Add("Defina a data de corte da obra: só entra como histórico o que é anterior a ela.");
        if (depois.Count > 0) avisos.Add($"{depois.Count} linhas de custo (R$ {depois.Sum(a => a.Valor).ToString("N2", Br)}) são da data de corte em diante e não entram: lance no sistema.");
        var fases = DeParaFases(cx);
        if (fases.FirstOrDefault(f => f.DestinoTexto == CustoRealizado.FaseAApropriar) is { } semFase)
            avisos.Add($"R$ {fases.Where(f => f.DestinoTexto == CustoRealizado.FaseAApropriar).Sum(f => f.Valor).ToString("N2", Br)} ficam \"{CustoRealizado.FaseAApropriar}\" (fase vazia ou sem correspondência) — classifique na aba Fases ou depois, no Custo Realizado.");
        if (cx.Locais.Count == 0) avisos.Add("A obra não tem locais (torre/pavimentos): todo o custo entra como \"obra toda\". Gere a estrutura antes, se quiser custo por pavimento.");
        if (cx.Ins.Count == 0) avisos.Add("Sem o relatório de insumos cadastrados, os produtos novos entram com unidade UN — envie o cadastro de insumos para trazer a unidade e a categoria.");
        return new ResumoHistoricoErp(cx.Lote.Id, cx.Obra.Id, cx.Obra.Nome, corte, cx.Lote.Status, cx.Arquivos,
            cx.Apr.Count == 0 ? null : cx.Apr.Min(a => a.Data), cx.Apr.Count == 0 ? null : cx.Apr.Max(a => a.Data),
            antes.Count, antes.Sum(a => a.Valor), depois.Count, depois.Sum(a => a.Valor), jaCustos,
            ocs.Count, cx.Oc.Count, cx.Oc.Sum(o => o.Total), jaPedidos, ocs.Count(g => g.Any(o => o.QtdRecebida < o.Qtd)),
            DeParaProdutos(cx), DeParaFornecedores(cx), fases, DeParaLocais(cx), avisos);
    }

    /// <summary>Grava a decisão do de-para (vale para esta e para as próximas importações).</summary>
    public async Task SalvarVinculoAsync(Guid obraId, TipoVinculoOrigem tipo, string chave, string texto, AcaoDePara acao, Guid? destinoId, string? destinoTexto,
        CancellationToken ct = default)
    {
        if (acao == AcaoDePara.Vincular && tipo is TipoVinculoOrigem.Produto or TipoVinculoOrigem.Fornecedor && destinoId is null)
            throw new RegraDeNegocioException("Escolha o cadastro para vincular.");
        if (tipo == TipoVinculoOrigem.Fase && string.IsNullOrWhiteSpace(destinoTexto)) throw new RegraDeNegocioException("Escolha a fase.");
        await using var db = fabrica.Criar();
        Guid? obra = tipo == TipoVinculoOrigem.Local ? obraId : null;
        var v = await db.VinculosOrigem.FirstOrDefaultAsync(x => x.Sistema == Sistema && x.Tipo == tipo && x.ObraId == obra && x.Chave == chave, ct);
        if (v is null) db.VinculosOrigem.Add(v = new VinculoOrigem { Sistema = Sistema, Tipo = tipo, ObraId = obra, Chave = chave });
        v.TextoOrigem = Cortar(texto, 400);
        v.Ignorar = acao == AcaoDePara.Ignorar && tipo != TipoVinculoOrigem.Local;
        v.DestinoId = acao == AcaoDePara.Vincular ? destinoId : null;
        v.DestinoTexto = tipo == TipoVinculoOrigem.Fase ? destinoTexto : null;
        await db.SaveChangesAsync(ct);
    }

    // ───────────── 3. Confirmação ─────────────

    public async Task<ResultadoCargaErp> ConfirmarAsync(Guid loteId, CancellationToken ct = default)
    {
        ResultadoCargaErp? res = null;
        await fabrica.EmTransacaoAsync(async db =>
        {
            var cx = await CarregarAsync(db, loteId, ct);
            if (cx.Obra.DataCorteSistema is not { } corte) throw new RegraDeNegocioException("Defina a data de corte da obra antes de confirmar.");
            if (cx.Lote.Status == StatusImportacao.Cancelada) throw new RegraDeNegocioException("Importação cancelada.");
            int prodCriados = 0, fornCriados = 0, ignorados = 0;

            // Produtos: vínculo gravado/sugerido ou cadastro novo ("A Cadastrar" se a categoria não casar).
            var produtos = new Dictionary<string, Guid?>();
            var cache = new CodigosProdutoServico.Cache();
            foreach (var item in DeParaProdutos(cx))
            {
                Guid? id = item.Acao switch { AcaoDePara.Vincular => item.DestinoId, AcaoDePara.Ignorar => null, _ => null };
                if (item.Acao == AcaoDePara.Criar)
                {
                    var ins = InsumoDe(cx, item.Chave);
                    var tipoTxt = ins?.TipoCusto ?? cx.Apr.FirstOrDefault(a => ChaveProduto(a.Insumo) == item.Chave)?.TipoCusto;
                    var categoria = ins?.Categoria ?? cx.Apr.FirstOrDefault(a => ChaveProduto(a.Insumo) == item.Chave)?.Categoria;
                    var p = new Produto
                    {
                        Nome = item.Texto, Unidade = UnidadesPadrao.Normalizar(ins?.Unidade), TipoCusto = SugestoesImportacao.TipoCustoDe(tipoTxt),
                        Grupo = SugestoesImportacao.GrupoDaCategoria(categoria, cx.Grupos), OrigemSistema = Sistema, CodigoErp = Cortar(ins?.Codigo, 60),
                        PrecoReferencia = ins?.Preco is > 0 ? ins.Preco : null, Ativo = true, UltimaImportacaoId = cx.Lote.Id
                    };
                    if (p.Grupo != "A Cadastrar") p.SubGrupo = "A Cadastrar";
                    p.Codigo = await CodigosProdutoServico.GerarAsync(db, p, cache, ct);
                    db.Produtos.Add(p);
                    cx.Produtos.Add(new SugestoesImportacao.Candidato(p.Id, p.Nome, p.Unidade)); // unidade do produto novo vale para pedido e custo
                    id = p.Id; prodCriados++;
                    await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Produto, item.Chave, item.Texto, p.Id, null, ct);
                }
                else if (item.Acao == AcaoDePara.Vincular && !item.Salvo)
                    await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Produto, item.Chave, item.Texto, item.DestinoId, null, ct);
                if (item.Acao == AcaoDePara.Ignorar) ignorados++;
                produtos[item.Chave] = id;
            }

            // Fornecedores: CPF/CNPJ do relatório de fornecedores ou nome; sem cadastro → novo, em qualificação.
            var fornecedores = new Dictionary<string, Guid?>();
            foreach (var item in DeParaFornecedores(cx))
            {
                Guid? id = item.Acao == AcaoDePara.Vincular ? item.DestinoId : null;
                if (item.Acao == AcaoDePara.Criar)
                {
                    var reg = cx.FornPorChave.GetValueOrDefault(item.Chave);
                    var doc = DocumentoDe(cx, item.Chave);
                    var (cidade, uf) = CidadeUf(reg?.CidadeUf);
                    var f = new Fornecedor
                    {
                        RazaoSocial = Cortar(reg?.RazaoSocial ?? item.Texto, 200)!, NomeFantasia = Cortar(reg?.Fantasia, 200), CpfCnpj = doc is null ? "" : FormatarDoc(doc),
                        Cidade = Cortar(cidade, 100), Uf = uf, Telefones = Cortar(reg?.Telefone, 100), Emails = Cortar(reg?.Email, 300),
                        StatusQualificacao = StatusQualificacao.EmQualificacao, Ativo = true, UltimaImportacaoId = cx.Lote.Id
                    };
                    db.Fornecedores.Add(f);
                    id = f.Id; fornCriados++;
                    cx.Fornecedores.Add((f.Id, f.RazaoSocial, item.Chave, doc ?? ""));
                    await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Fornecedor, item.Chave, item.Texto, f.Id, null, ct);
                }
                else if (item.Acao == AcaoDePara.Vincular && !item.Salvo)
                    await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Fornecedor, item.Chave, item.Texto, item.DestinoId, null, ct);
                fornecedores[item.Chave] = id;
            }
            Guid? Forn(string? nome) => nome is null ? null : fornecedores.GetValueOrDefault(SugestoesImportacao.Chave(nome));
            Guid? Prod(string nome) => produtos.GetValueOrDefault(ChaveProduto(nome));

            var fases = DeParaFases(cx).ToDictionary(f => f.Chave, f => f.DestinoTexto ?? CustoRealizado.FaseAApropriar);
            var locais = DeParaLocais(cx).ToDictionary(l => l.Chave, l => l.DestinoId);
            foreach (var f in DeParaFases(cx).Where(f => !f.Salvo)) await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Fase, f.Chave, f.Texto, null, f.DestinoTexto, ct);
            foreach (var l in DeParaLocais(cx).Where(l => !l.Salvo)) await GravarVinculoAsync(db, cx, TipoVinculoOrigem.Local, l.Chave, l.Texto, l.DestinoId, null, ct);

            // Pedidos históricos (OC do ERP): itens com o recebido; OC com saldo fica viva para receber no sistema depois do corte.
            int pedidos = 0, itensPed = 0;
            var pedidoPorOc = await db.Pedidos.Where(p => p.ObraId == cx.Obra.Id && p.CodigoErp != null && p.CodigoErp.StartsWith("OC "))
                .ToDictionaryAsync(p => p.CodigoErp!, p => p.Id, ct);
            foreach (var g in cx.Oc.GroupBy(o => o.Oc))
            {
                var codigo = CodigoPedido(g.Key);
                if (pedidoPorOc.ContainsKey(codigo)) continue;
                var primeiro = g.First();
                if (Forn(primeiro.Fornecedor) is not { } fid) { ignorados++; continue; }
                var itens = g.GroupBy(o => (Chave: ChaveProduto(o.Item), o.Qtd, o.ValorUnitario)).Select(x => x.MaxBy(o => o.QtdRecebida)!).ToList();
                var ped = new Pedido
                {
                    ObraId = cx.Obra.Id, Numero = await Numerador.ProximoAsync(db, "Pedido", ct), FornecedorId = fid, CodigoErp = codigo,
                    CondicaoPagamento = Cortar(primeiro.Condicao, 100), Entrega = primeiro.Previsao ?? primeiro.Criacao ?? corte,
                    Frete = TipoFrete.NaoSeAplica, UltimaImportacaoId = cx.Lote.Id,
                    EmailsCopia = null, AprovadoDiretorPor = "Histórico do ERP", AprovadoDiretorEm = ToOffset(primeiro.Criacao),
                    EnviadoFornecedorEm = ToOffset(primeiro.Criacao)
                };
                foreach (var o in itens)
                {
                    var prodId = Prod(o.Item);
                    ped.Itens.Add(new PedidoItem
                    {
                        PedidoId = ped.Id, ProdutoId = prodId, Descricao = Cortar(SugestoesImportacao.LimparNome(o.Item), 300)!,
                        Unidade = prodId is { } pid ? cx.Produtos.FirstOrDefault(p => p.Id == pid)?.Unidade ?? UnidadesPadrao.Normalizar(InsumoDe(cx, ChaveProduto(o.Item))?.Unidade) : UnidadesPadrao.Normalizar(InsumoDe(cx, ChaveProduto(o.Item))?.Unidade),
                        Quantidade = o.Qtd, ValorUnitario = o.ValorUnitario, ValorTotal = o.Total, QuantidadeRecebidaAceita = Math.Min(o.QtdRecebida, o.Qtd)
                    });
                }
                ped.Status = ped.Itens.All(i => i.QuantidadeRecebidaAceita >= i.Quantidade) ? StatusPedido.Entregue
                    : ped.Itens.Any(i => i.QuantidadeRecebidaAceita > 0) ? StatusPedido.EntregueParcial : StatusPedido.EnviadoFornecedor;
                db.Pedidos.Add(ped);
                pedidoPorOc[codigo] = ped.Id;
                pedidos++; itensPed += ped.Itens.Count;
            }

            // Custo realizado (só antes do corte), ligado ao pedido pela OC.
            int custos = 0; decimal valor = 0;
            var antes = cx.Apr.Where(a => a.Data < corte).ToList();
            var chaves = ChavesCusto(antes);
            var existentes = (await db.CustosRealizados.Where(c => c.ObraId == cx.Obra.Id && c.Origem == OrigemCusto.ImportacaoErp).Select(c => c.ChaveOrigem!).ToListAsync(ct)).ToHashSet();
            foreach (var (a, chave) in chaves)
            {
                if (existentes.Contains(chave)) continue;
                db.CustosRealizados.Add(new CustoRealizado
                {
                    ObraId = cx.Obra.Id, Data = a.Data, Historico = true, TipoCusto = SugestoesImportacao.TipoCustoDe(a.TipoCusto),
                    Fase = fases.GetValueOrDefault(SugestoesImportacao.Chave(a.Fase)) ?? CustoRealizado.FaseAApropriar,
                    LocalId = locais.GetValueOrDefault(SugestoesImportacao.Chave(a.Servico)),
                    FornecedorId = Forn(a.Fornecedor), FornecedorNome = Cortar(a.Fornecedor, 200), ProdutoId = Prod(a.Insumo),
                    Descricao = Cortar(SugestoesImportacao.LimparNome(a.Insumo), 300)!, Unidade = Prod(a.Insumo) is { } pid ? cx.Produtos.FirstOrDefault(p => p.Id == pid)?.Unidade : null,
                    Quantidade = a.Qtd, Valor = Math.Round(a.Valor, 2), DocumentoTipo = a.Nf is null ? null : "NF", DocumentoNumero = Cortar(a.Nf, 60),
                    PedidoId = a.Oc is { } oc && pedidoPorOc.TryGetValue(CodigoPedido(oc), out var pg) ? pg : null,
                    Origem = OrigemCusto.ImportacaoErp, ChaveOrigem = chave, LoteImportacaoId = cx.Lote.Id,
                    Observacao = a.Doc is null ? null : $"Documento {a.Doc} no ERP" + (a.Oc is null ? "" : $" · OC {a.Oc}")
                });
                custos++; valor += Math.Round(a.Valor, 2);
            }

            var lote = await db.LotesImportacao.FirstAsync(l => l.Id == cx.Lote.Id, ct);
            lote.Status = StatusImportacao.Concluida; lote.ConfirmadoEm = relogio.GetUtcNow(); lote.ConfirmadoPor = fabrica.Usuario.Nome;
            lote.LinhasInserir = custos + itensPed; lote.LinhasSemAlteracao = chaves.Count - custos;
            await db.SaveChangesAsync(ct);
            res = new ResultadoCargaErp(custos, valor, pedidos, itensPed, prodCriados, fornCriados, ignorados);
        }, ct);
        return res!;
    }

    public async Task CancelarAsync(Guid loteId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var lote = await db.LotesImportacao.FirstAsync(l => l.Id == loteId, ct);
        if (lote.Status == StatusImportacao.Concluida) throw new RegraDeNegocioException("Importação já confirmada não é cancelada.");
        lote.Status = StatusImportacao.Cancelada;
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Apoio ─────────────

    private static async Task GravarVinculoAsync(IQualyHubDb db, Contexto cx, TipoVinculoOrigem tipo, string chave, string texto, Guid? destinoId, string? destinoTexto, CancellationToken ct)
    {
        Guid? obra = tipo == TipoVinculoOrigem.Local ? cx.Obra.Id : null;
        var v = db.VinculosOrigem.Local.FirstOrDefault(x => x.Tipo == tipo && x.ObraId == obra && x.Chave == chave)
                ?? await db.VinculosOrigem.FirstOrDefaultAsync(x => x.Sistema == Sistema && x.Tipo == tipo && x.ObraId == obra && x.Chave == chave, ct);
        if (v is null) db.VinculosOrigem.Add(v = new VinculoOrigem { Sistema = Sistema, Tipo = tipo, ObraId = obra, Chave = chave });
        v.TextoOrigem = Cortar(texto, 400)!; v.DestinoId = destinoId; v.DestinoTexto = destinoTexto; v.Ignorar = false;
    }

    /// <summary>Chave de origem por linha de apropriação (linhas idênticas no relatório ganham sequência para não se anularem).</summary>
    private static List<(RegApr Reg, string Chave)> ChavesCusto(IEnumerable<RegApr> linhas)
    {
        var vistos = new Dictionary<string, int>();
        var r = new List<(RegApr, string)>();
        foreach (var a in linhas)
        {
            var baseTxt = string.Join('|', a.Data.ToString("yyyyMMdd"), a.Doc, a.Nf, a.Oc, SugestoesImportacao.Chave(a.Fornecedor), SugestoesImportacao.Chave(a.Insumo),
                SugestoesImportacao.Chave(a.Fase), SugestoesImportacao.Chave(a.Servico), a.Qtd.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                a.Valor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            var seq = vistos[baseTxt] = vistos.GetValueOrDefault(baseTxt) + 1;
            r.Add((a, "ERP:" + Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(baseTxt + "#" + seq)))));
        }
        return r;
    }

    private static string CodigoPedido(string oc) => $"OC {oc}";
    private static string? SemZeros(string? s) => s is null ? null : s.TrimStart('0') is { Length: > 0 } t ? t : s;
    private static DateTimeOffset? ToOffset(DateOnly? d) => d is { } x ? new DateTimeOffset(x.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
    private static string? Cortar(string? s, int max) => s is null ? null : s.Length > max ? s[..max] : s;
    private static string FormatarDoc(string d) => d.Length == 14 ? Conversor.FormatarCnpj(d) : d.Length == 11 ? Convert.ToUInt64(d).ToString(@"000\.000\.000\-00") : d;

    private static (string? Cidade, string? Uf) CidadeUf(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return (null, null);
        var partes = texto.Split(['/', '-'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return partes.Length >= 2 && partes[^1].Length == 2 ? (string.Join(" ", partes[..^1]), partes[^1].ToUpperInvariant()) : (texto.Trim(), null);
    }
}
