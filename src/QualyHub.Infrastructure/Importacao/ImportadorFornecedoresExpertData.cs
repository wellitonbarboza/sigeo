using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>Uma linha da planilha já interpretada (prévia).</summary>
public sealed record LinhaFornecedorPlanilha(int Linha, string Documento, string Nome, string Acao, IReadOnlyList<string> Erros, IReadOnlyList<string> Alteracoes);

public sealed record PreviaImportacaoFornecedores(IReadOnlyList<LinhaFornecedorPlanilha> Linhas)
{
    public int Novos => Linhas.Count(l => l.Acao == "Inserir");
    public int Atualizar => Linhas.Count(l => l.Acao == "Atualizar");
    public int SemAlteracao => Linhas.Count(l => l.Acao == "Sem alteração");
    public int ComErro => Linhas.Count(l => l.Acao == "Erro");
}

/// <summary>
/// Importa a planilha "Export" do cadastro de fornecedores do ExpertData (colunas CPF / CNPJ, NOME, CEP, CIDADE, UF,
/// ENDERECO, NUMERO, COMPLEMENTO, BAIRRO, TELEFONES, E-MAILS, NATUREZA JURÍDICA, PORTE, SITUAÇÃO CADASTRAL, CNAE,
/// CAPITAL SOCIAL, FUNDAÇÃO, CRIADOR POR, CRIAÇÃO, MODIFICADO POR, MODIFICAÇÃO, FANTASIA, IE, IM, TELEFONE0800).
/// Regras: chave = CPF/CNPJ (só dígitos, com DV válido); campo vazio na planilha nunca apaga dado do QualyHub;
/// valores-sentinela do sistema de origem (capital 1, fundação 01/01/2001 ou 0001, "NAO CONSTA") viram vazio;
/// tipo de fornecimento e qualificação NUNCA vêm da importação (decisão da Qualidade/Suprimentos).
/// </summary>
public sealed class ImportadorFornecedoresExpertData(FabricaDb fabrica)
{
    private static readonly CultureInfo Br = new("pt-BR");

    private sealed record Registro(int Linha, string Doc, Dictionary<string, string?> C);

    private static string Norm(string s) => new string(s.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();

    private static List<Registro> Ler(Stream arquivo)
    {
        using var wb = new XLWorkbook(arquivo);
        var ws = wb.Worksheets.First();
        var cab = ws.Row(1).CellsUsed().ToDictionary(c => Norm(c.GetString()), c => c.Address.ColumnNumber);
        if (!cab.ContainsKey("CPFCNPJ") || !cab.ContainsKey("NOME"))
            throw new RegraDeNegocioException("Planilha fora do padrão: precisa das colunas 'CPF / CNPJ' e 'NOME' na primeira linha.");
        var lista = new List<Registro>();
        var ultima = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= ultima; r++)
        {
            var row = ws.Row(r);
            string? V(string col)
            {
                if (!cab.TryGetValue(col, out var i)) return null;
                var cel = row.Cell(i);
                if (cel.IsEmpty()) return null;
                var t = cel.DataType == XLDataType.Number ? cel.GetDouble().ToString(CultureInfo.InvariantCulture) : cel.GetFormattedString().Trim();
                return string.IsNullOrWhiteSpace(t) ? null : t;
            }
            var campos = new Dictionary<string, string?>
            {
                ["doc"] = V("CPFCNPJ"), ["nome"] = V("NOME"), ["cep"] = V("CEP"), ["cidade"] = V("CIDADE"), ["uf"] = V("UF"),
                ["endereco"] = V("ENDERECO"), ["numero"] = V("NUMERO"), ["complemento"] = V("COMPLEMENTO"), ["bairro"] = V("BAIRRO"),
                ["telefones"] = V("TELEFONES"), ["emails"] = V("EMAILS"), ["natureza"] = V("NATUREZAJURIDICA"), ["porte"] = V("PORTE"),
                ["situacao"] = V("SITUACAOCADASTRAL"), ["cnae"] = V("CNAE"), ["capital"] = V("CAPITALSOCIAL"), ["fundacao"] = V("FUNDACAO"),
                ["criadoPor"] = V("CRIADORPOR") ?? V("CRIADOPOR"), ["criadoEm"] = V("CRIACAO"), ["alteradoPor"] = V("MODIFICADOPOR"),
                ["alteradoEm"] = V("MODIFICACAO"), ["fantasia"] = V("FANTASIA"), ["ie"] = V("IE"), ["im"] = V("IM"), ["t0800"] = V("TELEFONE0800"),
            };
            if (campos.Values.All(v => v is null)) continue;
            lista.Add(new Registro(r, Conversor.SomenteDigitos(campos["doc"]), campos));
        }
        return lista;
    }

    private static string? Limpo(string? v) => v is null || v.Equals("NAO CONSTA", StringComparison.OrdinalIgnoreCase) || v.Equals("Não informado", StringComparison.OrdinalIgnoreCase) ? null : v.Trim();

    private static decimal? Capital(string? v) =>
        decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d > 1 ? d : null;

    private static DateOnly? DataBr(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var s = v.Split(' ')[0];
        return DateOnly.TryParseExact(s, ["dd/MM/yyyy", "dd/MM/yy", "d/M/yyyy"], Br, DateTimeStyles.None, out var d) && d.Year > 1900 && d != new DateOnly(2001, 1, 1) ? d : null;
    }

    private static DateTimeOffset? DataHoraBr(string? v) =>
        DateTime.TryParseExact(v, ["dd/MM/yy HH:mm", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"], Br, DateTimeStyles.None, out var d)
            ? new DateTimeOffset(d, TimeSpan.FromHours(-3)) : null;

    private static string Formatar(string d) => d.Length == 14 ? Convert.ToUInt64(d).ToString(@"00\.000\.000\/0000\-00") : d.Length == 11 ? Convert.ToUInt64(d).ToString(@"000\.000\.000\-00") : d;

    /// <summary>Aplica os campos da planilha; devolve a lista do que mudou (vazio na planilha não apaga).</summary>
    private static List<string> Aplicar(Fornecedor f, Dictionary<string, string?> c)
    {
        var mud = new List<string>();
        void S(string rotulo, string? novo, Func<string?> get, Action<string?> set)
        {
            novo = Limpo(novo);
            if (novo is null || string.Equals(novo, get(), StringComparison.Ordinal)) return;
            set(novo); mud.Add(rotulo);
        }
        S("Razão social", c["nome"], () => f.RazaoSocial, v => f.RazaoSocial = v!);
        S("Fantasia", c["fantasia"], () => f.NomeFantasia, v => f.NomeFantasia = v);
        S("CEP", c["cep"], () => f.Cep, v => f.Cep = v);
        S("Endereço", c["endereco"], () => f.Endereco, v => f.Endereco = v);
        S("Número", c["numero"], () => f.Numero, v => f.Numero = v);
        S("Complemento", c["complemento"], () => f.Complemento, v => f.Complemento = v);
        S("Bairro", c["bairro"], () => f.Bairro, v => f.Bairro = v);
        S("Cidade", c["cidade"], () => f.Cidade, v => f.Cidade = v);
        S("UF", c["uf"]?.ToUpperInvariant(), () => f.Uf, v => f.Uf = v);
        S("Telefones", c["telefones"], () => f.Telefones, v => f.Telefones = v);
        S("E-mails", c["emails"], () => f.Emails, v => f.Emails = v);
        S("Natureza jurídica", c["natureza"], () => f.NaturezaJuridica, v => f.NaturezaJuridica = v);
        S("Porte", c["porte"], () => f.Porte, v => f.Porte = v);
        S("Situação cadastral", c["situacao"], () => f.SituacaoCadastral, v => f.SituacaoCadastral = v);
        S("CNAE", c["cnae"], () => f.Cnae, v => f.Cnae = v);
        S("IE", c["ie"], () => f.InscricaoEstadual, v => f.InscricaoEstadual = v);
        S("IM", c["im"], () => f.InscricaoMunicipal, v => f.InscricaoMunicipal = v);
        S("Telefone 0800", c["t0800"], () => f.Telefone0800, v => f.Telefone0800 = v);
        if (Capital(c["capital"]) is { } cap && cap != f.CapitalSocial) { f.CapitalSocial = cap; mud.Add("Capital social"); }
        if (DataBr(c["fundacao"]) is { } fund && fund != f.DataFundacao) { f.DataFundacao = fund; mud.Add("Fundação"); }
        // Empresário individual/MEI: a Receita grava a razão social como "12.345.678 NOME DA PESSOA".
        if (!f.Mei && System.Text.RegularExpressions.Regex.IsMatch(c["nome"] ?? "", @"^\d{2}\.\d{3}\.\d{3} ")) { f.Mei = true; mud.Add("MEI"); }
        f.OrigemSistema ??= "ExpertData";
        f.OrigemCriadoPor = Limpo(c["criadoPor"]) ?? f.OrigemCriadoPor;
        f.OrigemCriadoEm = DataHoraBr(c["criadoEm"]) ?? f.OrigemCriadoEm;
        f.OrigemAlteradoPor = Limpo(c["alteradoPor"]) ?? f.OrigemAlteradoPor;
        f.OrigemAlteradoEm = DataHoraBr(c["alteradoEm"]) ?? f.OrigemAlteradoEm;
        return mud;
    }

    private static List<string> Validar(Registro r, HashSet<string> vistos)
    {
        var e = new List<string>();
        if (!(Conversor.CnpjValido(r.Doc) || Conversor.CpfValido(r.Doc))) e.Add($"CPF/CNPJ inválido ('{r.C["doc"]}')");
        if (string.IsNullOrWhiteSpace(r.C["nome"])) e.Add("Nome vazio");
        if (r.Doc.Length > 0 && !vistos.Add(r.Doc)) e.Add("Duplicado na planilha");
        return e;
    }

    public async Task<PreviaImportacaoFornecedores> PreverAsync(Stream arquivo, CancellationToken ct = default)
    {
        var regs = Ler(arquivo);
        await using var db = fabrica.Criar();
        var atuais = (await db.Fornecedores.AsNoTracking().ToListAsync(ct)).GroupBy(f => Conversor.SomenteDigitos(f.CpfCnpj)).ToDictionary(g => g.Key, g => g.First());
        var vistos = new HashSet<string>();
        var linhas = new List<LinhaFornecedorPlanilha>();
        foreach (var r in regs)
        {
            var e = Validar(r, vistos);
            if (e.Count > 0) { linhas.Add(new(r.Linha, r.C["doc"] ?? "", r.C["nome"] ?? "", "Erro", e, [])); continue; }
            if (atuais.TryGetValue(r.Doc, out var f))
            {
                var mud = Aplicar(f, r.C);
                linhas.Add(new(r.Linha, Formatar(r.Doc), r.C["nome"]!, mud.Count > 0 ? "Atualizar" : "Sem alteração", [], mud));
            }
            else linhas.Add(new(r.Linha, Formatar(r.Doc), r.C["nome"]!, "Inserir", [], []));
        }
        return new PreviaImportacaoFornecedores(linhas);
    }

    /// <summary>Grava inserções e atualizações válidas numa única transação (auditada). Linhas com erro são ignoradas.</summary>
    public async Task<(int Inseridos, int Atualizados)> ImportarAsync(Stream arquivo, CancellationToken ct = default)
    {
        var regs = Ler(arquivo);
        await using var db = fabrica.Criar();
        var atuais = (await db.Fornecedores.ToListAsync(ct)).GroupBy(f => Conversor.SomenteDigitos(f.CpfCnpj)).ToDictionary(g => g.Key, g => g.First());
        var vistos = new HashSet<string>();
        int ins = 0, upd = 0;
        foreach (var r in regs)
        {
            if (Validar(r, vistos).Count > 0) continue;
            if (atuais.TryGetValue(r.Doc, out var f))
            {
                if (Aplicar(f, r.C).Count > 0) upd++;
            }
            else
            {
                f = new Fornecedor { CpfCnpj = Formatar(r.Doc), RazaoSocial = r.C["nome"]!.Trim(), StatusQualificacao = StatusQualificacao.EmQualificacao, Ativo = true };
                Aplicar(f, r.C);
                db.Fornecedores.Add(f);
                atuais[r.Doc] = f;
                ins++;
            }
        }
        await db.SaveChangesAsync(ct);
        return (ins, upd);
    }
}
