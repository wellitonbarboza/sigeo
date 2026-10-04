using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Infrastructure.Importacao;

public sealed record ResultadoEspecificacao(int Materiais, int MateriaisNovos, int TiposDocumento, int Ensaios, int GruposLigados);
public sealed record ResultadoMapa(int Lidos, int Movidos, int Controlados, int NaoEncontrados, int GruposCriados, ResultadoPadronizacao? Codigos, IReadOnlyList<string> Amostra);

/// <summary>
/// Importa a Tabela de Especificação e Ensaios de Materiais (planilha estruturada da DT.SUP.01.01: abas "1 Materiais", "2 Documentos",
/// "3 Ensaios", "4 Cadastro de documentos") e aplica o Mapa do catálogo (aba "Produtos": grupo atual → grupo novo, com a coluna de decisão).
/// </summary>
public sealed class ImportadorEspecificacao(FabricaDb fabrica, CodigosProdutoServico codigos)
{
    private static string N(string? s) => string.Join(' ', new string((s ?? "").Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static IXLWorksheet Aba(XLWorkbook wb, string inicio) =>
        wb.Worksheets.FirstOrDefault(w => N(w.Name).StartsWith(N(inicio), StringComparison.Ordinal) || N(w.Name).Contains(N(inicio)))
        ?? throw new RegraDeNegocioException($"Planilha sem a aba '{inicio}'.");

    private static List<Dictionary<string, string?>> Linhas(IXLWorksheet ws)
    {
        var cab = ws.Row(1).CellsUsed().ToDictionary(c => c.Address.ColumnNumber, c => N(c.GetString()));
        var r = new List<Dictionary<string, string?>>();
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var i = 2; i <= ultima; i++)
        {
            var row = ws.Row(i);
            var d = cab.ToDictionary(c => c.Value, c => row.Cell(c.Key).IsEmpty() ? null : row.Cell(c.Key).GetFormattedString().Trim() is { Length: > 0 } t ? t : null);
            if (d.Values.Any(v => v is not null)) r.Add(d);
        }
        return r;
    }

    private static string? V(Dictionary<string, string?> d, params string[] prefixos) =>
        prefixos.Select(p => d.FirstOrDefault(k => k.Key.StartsWith(N(p), StringComparison.Ordinal)).Value).FirstOrDefault(v => v is not null);

    private static FrequenciaDocumento Frequencia(string? momento, int? validade) => N(momento) switch
    {
        var m when m.Contains("ENTREGA") || m.Contains("LOTE") || m.Contains("COLETA") => FrequenciaDocumento.PorEntrega,
        var m when m.Contains("OBRA") || m.Contains("CONTRATO") => FrequenciaDocumento.PorContrato,
        var m when m.Contains("MENSAL") => FrequenciaDocumento.Mensal,
        var m when m.Contains("ANUAL") => FrequenciaDocumento.Anual,
        _ => validade is > 0 ? FrequenciaDocumento.Validade : FrequenciaDocumento.NoCadastro
    };

    public async Task<ResultadoEspecificacao> ImportarEspecificacaoAsync(Stream arquivo, CancellationToken ct = default)
    {
        using var wb = Planilha.Abrir(arquivo);
        var mats = Linhas(Aba(wb, "1 Materiais"));
        var docs = Linhas(Aba(wb, "2 Documentos"));
        var ens = Linhas(Aba(wb, "3 Ensaios"));
        var cad = wb.Worksheets.Any(w => N(w.Name).Contains("CADASTRO DE DOCUMENTOS")) ? Linhas(Aba(wb, "4 Cadastro de documentos")) : [];
        await using var db = fabrica.Criar();
        var existentes = await db.MateriaisControlados.ToListAsync(ct);
        var grupos = await db.CategoriasProduto.Where(c => c.PaiId == null).ToListAsync(ct);
        var tipos = await db.TiposDocumento.Where(t => t.AplicaA == AplicaA.Fornecedor).ToListAsync(ct);
        int novos = 0, ligados = 0, novosTipos = 0;
        var criadosAgora = new HashSet<TipoDocumentoExigido>();
        foreach (var c in cad)
        {
            var nome = V(c, "TIPO DE DOCUMENTO");
            if (nome is null || nome.StartsWith('(')) continue;
            int? val = int.TryParse(V(c, "VALIDADE"), out var vd) ? vd : null;
            var t = tipos.FirstOrDefault(x => N(x.Nome) == N(nome));
            if (t is not null && !criadosAgora.Contains(t))
            {
                // Tipo já existente (cadastro padrão ou ajustado pelo usuário): a planilha só completa a validade que faltava.
                t.ValidadePadraoDias ??= val;
                continue;
            }
            if (t is null) { t = new TipoDocumentoExigido { AplicaA = AplicaA.Fornecedor, Nome = nome, ExigidoPara = TipoFornecimento.Material, Categoria = "Técnico" }; db.TiposDocumento.Add(t); tipos.Add(t); criadosAgora.Add(t); novosTipos++; }
            t.ValidadePadraoDias = val ?? t.ValidadePadraoDias;
            t.Frequencia = Frequencia(V(c, "MOMENTO"), t.ValidadePadraoDias);
            t.PorEntrega = t.Frequencia == FrequenciaDocumento.PorEntrega;
            t.BloqueiaQuandoVencido = N(V(c, "BLOQUEIA")) != "NAO";
            t.Obrigatorio = false; // exigido pelo material, não de todo fornecedor
        }
        foreach (var m in mats)
        {
            var cod = V(m, "CODIGO");
            var nome = V(m, "MATERIAL");
            if (cod is null || nome is null) continue;
            cod = cod.PadLeft(2, '0');
            var x = existentes.FirstOrDefault(e => e.Codigo == cod);
            if (x is null) { x = new MaterialControlado { Codigo = cod }; db.MateriaisControlados.Add(x); existentes.Add(x); novos++; }
            x.Nome = nome;
            x.EspecificacaoModelo = V(m, "ESPECIFICACAO") ?? x.EspecificacaoModelo;
            x.GrupoCatalogo = V(m, "GRUPO DO CATALOGO") ?? x.GrupoCatalogo;
            x.SubGrupos = V(m, "SUBGRUPOS") ?? x.SubGrupos;
            x.ConsultarPsq = N(V(m, "CONSULTAR PSQ")) == "SIM";
            x.ConstaPsq = x.ConsultarPsq ? "Sim" : "Não";
            var ds = docs.Where(d => V(d, "CODIGO")?.PadLeft(2, '0') == cod).ToList();
            if (ds.Count > 0)
                x.DocumentosExigidos = string.Join("\n", ds.Select(d => $"• {V(d, "TIPO DE DOCUMENTO") ?? V(d, "TEXTO")} ({V(d, "MOMENTO") ?? "—"}){(V(d, "OBSERVACAO") is { } o ? " — " + o : "")}"));
            foreach (var d in ds)
            {
                var tn = V(d, "TIPO DE DOCUMENTO");
                if (tn is null || tn.StartsWith('(') || tipos.Any(t => N(t.Nome) == N(tn))) continue;
                var t = new TipoDocumentoExigido { AplicaA = AplicaA.Fornecedor, Nome = tn, ExigidoPara = TipoFornecimento.Material, Obrigatorio = false, Categoria = "Técnico", Frequencia = Frequencia(V(d, "MOMENTO"), null) };
                t.PorEntrega = t.Frequencia == FrequenciaDocumento.PorEntrega;
                db.TiposDocumento.Add(t); tipos.Add(t); criadosAgora.Add(t); novosTipos++;
            }
            var es = ens.Where(e => V(e, "CODIGO")?.PadLeft(2, '0') == cod).ToList();
            if (es.Count > 0)
                x.Ensaios = string.Join("\n", es.GroupBy(e => (V(e, "FINALIDADE"), V(e, "NORMA"))).Select(g =>
                    $"{g.Key.Item1 ?? "Ensaio"}{(g.Key.Item2 is null ? "" : " · " + g.Key.Item2)}: {string.Join("; ", g.Select(e => (V(e, "SUBDIVISAO") is { } sd ? sd + " – " : "") + V(e, "ENSAIO")))}"));
            // Grupo ligado só quando o catálogo já tem o grupo com o nome do material (1 grupo = 1 material);
            // o mapa do catálogo faz a ligação pelo código quando os nomes diferem.
            var g = grupos.FirstOrDefault(gr => N(gr.Nome) == N(nome));
            if (g is not null && !existentes.Any(e => e != x && e.CategoriaProdutoId == g.Id)) { x.CategoriaProdutoId = g.Id; ligados++; }
        }
        await db.SaveChangesAsync(ct);
        return new ResultadoEspecificacao(mats.Count, novos, novosTipos, ens.Count, ligados);
    }

    /// <summary>
    /// Aplica o mapa do catálogo: move cada produto para o grupo novo (decisão do usuário prevalece sobre a sugestão), cria os grupos
    /// que faltam, liga cada grupo controlado ao material da tabela (coluna Cód.) e padroniza os códigos (produto já usado mantém o código).
    /// </summary>
    public async Task<ResultadoMapa> AplicarMapaAsync(Stream arquivo, bool aplicar, CancellationToken ct = default)
    {
        using var wb = Planilha.Abrir(arquivo);
        var linhas = Linhas(Aba(wb, "Produtos"));
        var gruposCtrl = wb.Worksheets.Any(w => N(w.Name).Contains("GRUPOS CONTROLADOS")) ? Linhas(Aba(wb, "Grupos controlados")) : [];
        var dup = gruposCtrl.Where(g => V(g, "GRUPO NOVO") is not null).GroupBy(g => N(V(g, "GRUPO NOVO"))).Where(g => g.Count() > 1).Select(g => V(g.First(), "GRUPO NOVO")).ToList();
        if (dup.Count > 0) throw new RegraDeNegocioException($"Aba Grupos controlados: grupo repetido ({string.Join(", ", dup)}). Deixe uma linha por grupo.");
        var codPorGrupo = gruposCtrl.Where(g => V(g, "GRUPO NOVO") is not null && V(g, "COD") is not null)
                                    .ToDictionary(g => N(V(g, "GRUPO NOVO")), g => V(g, "COD")!.PadLeft(2, '0'));
        await using var db = fabrica.Criar();
        var produtos = await db.Produtos.ToListAsync(ct);
        var porChave = produtos.GroupBy(p => N(p.Nome) + "|" + ImportadorProdutosExpertData.Unidade(p.Unidade)).ToDictionary(g => g.Key, g => g.ToList());
        var porNome = produtos.GroupBy(p => N(p.Nome)).ToDictionary(g => g.Key, g => g.ToList());
        var materiais = await db.MateriaisControlados.ToListAsync(ct);
        var cache = new CodigosProdutoServico.Cache();
        int movidos = 0, ctrl = 0, nao = 0;
        var amostra = new List<string>();
        foreach (var l in linhas)
        {
            var desc = V(l, "DESCRICAO");
            if (desc is null) continue;
            var atual = V(l, "GRUPO ATUAL");
            var novo = V(l, "SUA DECISAO") ?? V(l, "GRUPO NOVO", "GRUPO APOS AJUSTE");
            if (novo is null) continue;
            var lista = porChave.GetValueOrDefault(N(desc) + "|" + ImportadorProdutosExpertData.Unidade(V(l, "UN"))) ?? porNome.GetValueOrDefault(N(desc));
            var ps = lista?.Where(p => atual is null || N(p.Grupo) == N(atual)).ToList() ?? [];
            if (ps.Count == 0) ps = lista ?? [];
            if (ps.Count == 0) { nao++; continue; }
            var cod = V(l, "SUA DECISAO") is not null ? codPorGrupo.GetValueOrDefault(N(novo)) : V(l, "COD")?.PadLeft(2, '0') ?? codPorGrupo.GetValueOrDefault(N(novo));
            foreach (var p in ps)
            {
                var sub = string.IsNullOrWhiteSpace(p.SubGrupo) || N(p.SubGrupo) == "A CADASTRAR" ? "Geral" : p.SubGrupo!;
                if (N(p.Grupo) != N(novo))
                {
                    if (amostra.Count < 25) amostra.Add($"{p.Nome}: {p.Grupo} › {p.SubGrupo} → {novo} › {sub}");
                    p.Grupo = novo.Trim(); p.SubGrupo = sub; movidos++;
                    await CodigosProdutoServico.GarantirCategoriasAsync(db, p.Grupo, p.SubGrupo, cache, ct);
                }
                if (cod is not null && materiais.FirstOrDefault(m => m.Codigo == cod) is { } mat)
                {
                    p.MaterialControladoId = mat.Id; ctrl++;
                    var (g, _) = await CodigosProdutoServico.GarantirCategoriasAsync(db, p.Grupo!, p.SubGrupo ?? "Geral", cache, ct);
                    // 1 grupo = 1 material: o grupo novo do mapa passa a ser o ligado (outro material que apontava para ele é desligado).
                    if (mat.CategoriaProdutoId != g.Id)
                    {
                        foreach (var outro in materiais.Where(m => m != mat && m.CategoriaProdutoId == g.Id)) outro.CategoriaProdutoId = null;
                        mat.CategoriaProdutoId = g.Id;
                    }
                }
            }
        }
        // Grupos controlados sem produto hoje (ex.: graute usinado) também são criados e ligados ao material.
        foreach (var (grupoNovo, cod) in gruposCtrl.Where(g => V(g, "GRUPO NOVO") is not null && V(g, "COD") is not null).Select(g => (V(g, "GRUPO NOVO")!, V(g, "COD")!.PadLeft(2, '0'))))
            if (materiais.FirstOrDefault(m => m.Codigo == cod) is { } mat && mat.CategoriaProdutoId is null)
            {
                var (g, _) = await CodigosProdutoServico.GarantirCategoriasAsync(db, grupoNovo.Trim(), "Geral", cache, ct);
                if (!materiais.Any(m => m != mat && m.CategoriaProdutoId == g.Id)) mat.CategoriaProdutoId = g.Id;
            }
        var criados = db.ChangeTracker.Entries<CategoriaProduto>().Count(e => e.State == EntityState.Added && e.Entity.PaiId == null);
        if (!aplicar) return new ResultadoMapa(linhas.Count, movidos, ctrl, nao, criados, null, amostra);
        await db.SaveChangesAsync(ct);
        var pad = await codigos.PadronizarAsync(true, ct);
        return new ResultadoMapa(linhas.Count, movidos, ctrl, nao, criados, pad, amostra);
    }

    /// <summary>Reaplica o vínculo grupo → material controlado em todos os produtos (depois de ligar/alterar grupos).</summary>
    public async Task<int> VincularPorGrupoAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var cache = new CodigosProdutoServico.Cache();
        var n = 0;
        foreach (var p in await db.Produtos.ToListAsync(ct))
        {
            var antes = p.MaterialControladoId;
            await CodigosProdutoServico.VincularControladoAsync(db, p, cache, ct);
            if (antes != p.MaterialControladoId) n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }
}
