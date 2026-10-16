using Sigeo.Application.Servicos;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Infrastructure.Servicos;

namespace Sigeo.Infrastructure.Importacao;

public sealed record LinhaProdutoPlanilha(int Linha, string Nome, string Unidade, string? Grupo, string? SubGrupo, string Acao, string? Detalhe);

public sealed record PreviaImportacaoProdutos(IReadOnlyList<LinhaProdutoPlanilha> Linhas, IReadOnlyDictionary<string, string> UnidadesConvertidas)
{
    public int Novos => Linhas.Count(l => l.Acao == "Inserir");
    public int Atualizar => Linhas.Count(l => l.Acao == "Atualizar");
    public int SemAlteracao => Linhas.Count(l => l.Acao == "Sem alteração");
    public int Duplicados => Linhas.Count(l => l.Acao == "Duplicado");
    public int ComErro => Linhas.Count(l => l.Acao == "Erro");
    public int AClassificar => Linhas.Count(l => l.Acao is "Inserir" or "Atualizar" && (l.Grupo is null || l.Grupo == "A Cadastrar"));
    public int Controlados => Linhas.Count(l => l.Detalhe?.Contains("controlado") == true);
}

/// <summary>
/// Importa a planilha "Export Produto Genérico" do ExpertData (GRUPO, SUB-GRUPO, DESCRIÇÃO GENÉRICA, UN, NCMID, DESCRIÇÃO COMPLETA).
/// Chave = descrição genérica normalizada + unidade padronizada (a planilha não tem código): repetidos no arquivo são ignorados.
/// Novos recebem o código padronizado da categoria (HID-CNX-0001) ou provisório (TMP-000001) se "A Cadastrar". Unidades equivalentes são padronizadas (UND/UNID → UN, PEC/PEÇA → PC, MT → M…).
/// NCM só é aceito com 8 dígitos e diferente de 00000000 (o export usa zero para "sem NCM"). Produto cujo sub-grupo está listado num material controlado é vinculado a ele automaticamente.
/// Campo vazio na planilha nunca apaga dado do Sigeo.
/// </summary>
public sealed class ImportadorProdutosExpertData(IFabricaDb fabrica)
{
    /// <summary>Unidade padronizada (mesma regra do cadastro: <see cref="UnidadesPadrao"/>).</summary>
    public static string Unidade(string? un) => UnidadesPadrao.Normalizar(un);

    public static string Chave(string nome, string un)
    {
        var n = new string(nome.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return string.Join(' ', n.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)) + "|" + un;
    }

    private static string SemAcento(string s) => new string(s.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Trim().ToUpperInvariant();

    private sealed record Registro(int Linha, string Nome, string Un, string UnOriginal, string? Grupo, string? Sub, string? Ncm, string? Completa);

    private static List<Registro> Ler(Stream arquivo)
    {
        using var wb = Planilha.Abrir(arquivo);
        var ws = wb.Worksheets.First();
        var cab = ws.Row(1).CellsUsed().ToDictionary(c => new string(SemAcento(c.GetString()).Where(char.IsLetterOrDigit).ToArray()), c => c.Address.ColumnNumber);
        if (!cab.ContainsKey("DESCRICAOGENERICA")) throw new RegraDeNegocioException("Planilha fora do padrão: falta a coluna 'DESCRIÇÃO GENÉRICA'.");
        var lista = new List<Registro>();
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= ultima; r++)
        {
            var row = ws.Row(r);
            string? V(string col) => cab.TryGetValue(col, out var i) && !row.Cell(i).IsEmpty() ? row.Cell(i).GetFormattedString().Trim() is { Length: > 0 } t ? t : null : null;
            var nome = V("DESCRICAOGENERICA");
            var grupo = V("GRUPO");
            if (nome is null && grupo is not null && grupo.All(char.IsDigit)) continue; // linha de total do export
            if (nome is null && V("UN") is null) continue;
            var ncm = new string((V("NCMID") ?? V("NCM") ?? "").Where(char.IsDigit).ToArray());
            var unO = V("UN") ?? "";
            lista.Add(new Registro(r, nome ?? "", Unidade(unO), unO, grupo, V("SUBGRUPO"), ncm.Length == 8 && ncm.Trim('0').Length > 0 ? ncm : null, V("DESCRICAOCOMPLETA")));
        }
        return lista;
    }

    private static Dictionary<string, Guid> MapaControlados(IEnumerable<MaterialControlado> mats)
    {
        var mapa = new Dictionary<string, Guid>();
        foreach (var m in mats.Where(m => m.Ativo && !string.IsNullOrWhiteSpace(m.SubGrupos)))
            foreach (var s in m.SubGrupos!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mapa.TryAdd(SemAcento(s.Replace('›', '>')), m.Id);
        return mapa;
    }

    private static Guid? Controlado(Dictionary<string, Guid> mapa, string? grupo, string? sub)
    {
        if (string.IsNullOrWhiteSpace(sub) || sub.Equals("A Cadastrar", StringComparison.OrdinalIgnoreCase)) return null;
        if (mapa.TryGetValue(SemAcento($"{grupo} > {sub}"), out var id)) return id;
        return mapa.TryGetValue(SemAcento(sub), out id) ? id : null;
    }

    private static List<string> Aplicar(Produto p, Registro r, Guid? controlado)
    {
        var mud = new List<string>();
        void S(string rot, string? novo, Func<string?> get, Action<string?> set) { if (novo is not null && novo != get()) { set(novo); mud.Add(rot); } }
        // Classificação da planilha só substitui "A Cadastrar"/vazio — não desfaz classificação feita no Sigeo.
        if (p.AClassificar || p.CriadoEm == default)
        {
            S("Grupo", r.Grupo, () => p.Grupo, v => p.Grupo = v);
            S("Subgrupo", r.Sub, () => p.SubGrupo, v => p.SubGrupo = v);
        }
        S("NCM", r.Ncm, () => p.Ncm, v => p.Ncm = v);
        S("Descrição completa", r.Completa, () => p.DescricaoCompleta, v => p.DescricaoCompleta = v);
        if (controlado is not null && p.MaterialControladoId is null) { p.MaterialControladoId = controlado; mud.Add("vinculado a material controlado"); }
        p.OrigemSistema ??= "ExpertData";
        return mud;
    }

    private async Task<(List<LinhaProdutoPlanilha> Linhas, Dictionary<string, string> Conv, List<(Registro R, Produto? Atual, Guid? Ctrl)> Validos)> AnalisarAsync(
        Stream arquivo, ISigeoDb db, bool rastrear, CancellationToken ct)
    {
        var regs = Ler(arquivo);
        var q = rastrear ? db.Produtos : db.Produtos.AsNoTracking();
        var atuais = (await q.ToListAsync(ct)).GroupBy(p => Chave(p.Nome, Unidade(p.Unidade))).ToDictionary(g => g.Key, g => g.First());
        var mapa = MapaControlados(await db.MateriaisControlados.AsNoTracking().ToListAsync(ct));
        var vistos = new Dictionary<string, int>();
        var linhas = new List<LinhaProdutoPlanilha>();
        var validos = new List<(Registro, Produto?, Guid?)>();
        var conv = new Dictionary<string, string>();
        foreach (var r in regs)
        {
            if (!string.Equals(r.UnOriginal, r.Un, StringComparison.OrdinalIgnoreCase) && r.UnOriginal.Length > 0) conv.TryAdd(r.UnOriginal.ToUpperInvariant(), r.Un);
            if (r.Nome.Length == 0) { linhas.Add(new(r.Linha, "", r.Un, r.Grupo, r.Sub, "Erro", "Descrição genérica vazia")); continue; }
            if (r.Nome.Length > 400) { linhas.Add(new(r.Linha, r.Nome[..60], r.Un, r.Grupo, r.Sub, "Erro", "Descrição acima de 400 caracteres")); continue; }
            var k = Chave(r.Nome, r.Un);
            if (vistos.TryGetValue(k, out var l1)) { linhas.Add(new(r.Linha, r.Nome, r.Un, r.Grupo, r.Sub, "Duplicado", $"igual à linha {l1}")); continue; }
            vistos[k] = r.Linha;
            var ctrl = Controlado(mapa, r.Grupo, r.Sub);
            var extra = ctrl is null ? null : "material controlado (vínculo automático)";
            if (atuais.TryGetValue(k, out var p))
            {
                var copia = rastrear ? p : new Produto { Grupo = p.Grupo, SubGrupo = p.SubGrupo, Ncm = p.Ncm, DescricaoCompleta = p.DescricaoCompleta, MaterialControladoId = p.MaterialControladoId, CriadoEm = p.CriadoEm, OrigemSistema = p.OrigemSistema };
                var mud = Aplicar(copia, r, ctrl);
                linhas.Add(new(r.Linha, r.Nome, r.Un, r.Grupo, r.Sub, mud.Count > 0 ? "Atualizar" : "Sem alteração", string.Join(", ", mud)));
                validos.Add((r, p, ctrl));
            }
            else
            {
                linhas.Add(new(r.Linha, r.Nome, r.Un, r.Grupo, r.Sub, "Inserir", extra));
                validos.Add((r, null, ctrl));
            }
        }
        return (linhas, conv, validos);
    }

    public async Task<PreviaImportacaoProdutos> PreverAsync(Stream arquivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var (linhas, conv, _) = await AnalisarAsync(arquivo, db, false, ct);
        return new PreviaImportacaoProdutos(linhas, conv);
    }

    public async Task<(int Inseridos, int Atualizados)> ImportarAsync(Stream arquivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var (_, _, validos) = await AnalisarAsync(arquivo, db, true, ct);
        db.ChangeTracker.AutoDetectChangesEnabled = false; // lote grande
        int ins = 0, upd = 0;
        var alterados = new List<Guid>();
        var cache = new CodigosProdutoServico.Cache();
        foreach (var (r, atual, ctrl) in validos)
        {
            if (atual is not null)
            {
                if (Aplicar(atual, r, ctrl).Count > 0) { upd++; alterados.Add(atual.Id); await CodigosProdutoServico.AplicarAoSalvarAsync(db, atual, cache, ct); }
                continue;
            }
            var p = new Produto { Nome = r.Nome.Trim(), Unidade = r.Un, Ativo = true };
            Aplicar(p, r, ctrl);
            p.Codigo = await CodigosProdutoServico.GerarAsync(db, p, cache, ct);
            db.Produtos.Add(p);
            ins++;
        }
        db.ChangeTracker.AutoDetectChangesEnabled = true;
        await db.SaveChangesAsync(ct);
        // Produtos alterados pela planilha valem também nas requisições, cotações e pedidos em aberto.
        if (await SincronizacaoCadastros.ProdutosAsync(db, alterados, ct) > 0) await db.SaveChangesAsync(ct);
        return (ins, upd);
    }
}
