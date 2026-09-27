using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Importacao;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Importacao;

/// <summary>
/// Importação de fornecedores do Obra Prima — padrão de referência para os demais importadores
/// (funcionários, pedidos, medições): 1) Preparar = staging + validação + prévia; 2) Confirmar = upsert.
/// Campos QualyHub esperados no layout: CpfCnpj, RazaoSocial, NomeFantasia, Email, Telefone, Cidade, Uf, Codigo.
/// </summary>
public sealed class ImportadorFornecedores(QualyHubDbContext db, TimeProvider relogio)
{
    public async Task<LoteImportacao> PrepararAsync(LoteImportacao lote, IReadOnlyList<LinhaLida> linhas, CancellationToken ct = default)
    {
        var existentes = await db.Fornecedores.AsNoTracking()
            .ToDictionaryAsync(f => f.CpfCnpj, ct);

        var vistos = new HashSet<string>();
        foreach (var l in linhas)
        {
            var doc = Conversor.SomenteDigitos(l["CpfCnpj"]);
            var erros = new List<string>();
            if (!(Conversor.CnpjValido(doc) || Conversor.CpfValido(doc))) erros.Add($"CPF/CNPJ inválido: '{l["CpfCnpj"]}'.");
            if (string.IsNullOrWhiteSpace(l["RazaoSocial"])) erros.Add("Razão social vazia.");
            if (!vistos.Add(doc)) erros.Add("CPF/CNPJ duplicado no arquivo.");

            var acao = erros.Count > 0 ? AcaoLinhaImportacao.Erro
                : existentes.TryGetValue(doc, out var atual) ? (Mudou(atual, l) ? AcaoLinhaImportacao.Atualizar : AcaoLinhaImportacao.SemAlteracao)
                : AcaoLinhaImportacao.Inserir;

            lote.Linhas.Add(new LinhaImportacao
            {
                NumeroLinha = l.NumeroLinha,
                ChaveNatural = doc,
                DadosJson = JsonSerializer.Serialize(l.Campos),
                Acao = acao,
                Erros = erros.Count > 0 ? string.Join(" ", erros) : null,
                EntidadeAfetadaId = existentes.TryGetValue(doc, out var e) ? e.Id : null
            });
        }

        lote.TotalLinhas = lote.Linhas.Count;
        lote.LinhasInserir = lote.Linhas.Count(x => x.Acao == AcaoLinhaImportacao.Inserir);
        lote.LinhasAtualizar = lote.Linhas.Count(x => x.Acao == AcaoLinhaImportacao.Atualizar);
        lote.LinhasSemAlteracao = lote.Linhas.Count(x => x.Acao == AcaoLinhaImportacao.SemAlteracao);
        lote.LinhasComErro = lote.Linhas.Count(x => x.Acao == AcaoLinhaImportacao.Erro);
        lote.Status = lote.LinhasComErro > 0 ? StatusImportacao.ComErros : StatusImportacao.AguardandoConfirmacao;
        return lote;
    }

    /// <summary>Aplica somente linhas válidas. Linhas com erro ficam no lote para correção e reenvio.</summary>
    public async Task ConfirmarAsync(Guid loteId, string confirmadoPor, CancellationToken ct = default)
    {
        var lote = await db.LotesImportacao.Include(x => x.Linhas).FirstAsync(x => x.Id == loteId, ct);
        if (lote.Status is not (StatusImportacao.AguardandoConfirmacao or StatusImportacao.ComErros))
            throw new InvalidOperationException("Lote não está aguardando confirmação.");

        var chaves = lote.Linhas.Where(x => x.Acao is AcaoLinhaImportacao.Inserir or AcaoLinhaImportacao.Atualizar)
                                .Select(x => x.ChaveNatural).ToList();
        var atuais = await db.Fornecedores.Where(f => chaves.Contains(f.CpfCnpj)).ToDictionaryAsync(f => f.CpfCnpj, ct);

        foreach (var linha in lote.Linhas.Where(x => x.Acao is AcaoLinhaImportacao.Inserir or AcaoLinhaImportacao.Atualizar))
        {
            var d = JsonSerializer.Deserialize<Dictionary<string, string?>>(linha.DadosJson)!;
            if (!atuais.TryGetValue(linha.ChaveNatural, out var f))
            {
                f = new Fornecedor { CpfCnpj = linha.ChaveNatural, EmpresaId = lote.EmpresaId };
                db.Fornecedores.Add(f);
            }
            Aplicar(f, d);
            f.UltimaImportacaoId = lote.Id;
            linha.EntidadeAfetadaId = f.Id;
        }

        lote.Status = StatusImportacao.Concluida;
        lote.ConfirmadoEm = relogio.GetUtcNow();
        lote.ConfirmadoPor = confirmadoPor;
        await db.SaveChangesAsync(ct);
    }

    private static void Aplicar(Fornecedor f, IReadOnlyDictionary<string, string?> d)
    {
        f.RazaoSocial = d.GetValueOrDefault("RazaoSocial")?.Trim() ?? f.RazaoSocial;
        f.NomeFantasia = Valor(d, "NomeFantasia") ?? f.NomeFantasia;
        f.Emails = Valor(d, "Email") ?? f.Emails;
        f.Telefones = Valor(d, "Telefone") ?? f.Telefones;
        f.Cidade = Valor(d, "Cidade") ?? f.Cidade;
        f.Uf = Valor(d, "Uf")?.ToUpperInvariant() ?? f.Uf;
        f.CodigoObraPrima = Valor(d, "Codigo") ?? f.CodigoObraPrima;
        // Situação SGQ (qualificação) NUNCA vem da importação: é decisão da Qualidade no QualyHub.
    }

    private static bool Mudou(Fornecedor f, LinhaLida l) =>
        f.RazaoSocial != l["RazaoSocial"]?.Trim()
        || (Valor(l.Campos, "Email") is { } e && e != f.Emails)
        || (Valor(l.Campos, "Telefone") is { } t && t != f.Telefones)
        || (Valor(l.Campos, "Cidade") is { } c && c != f.Cidade);

    private static string? Valor(IReadOnlyDictionary<string, string?> d, string k) =>
        d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
}

/// <summary>
/// LGPD — CPF de funcionário nunca em claro no banco: HMAC-SHA256 com chave do Key Vault (permite casar na importação)
/// + versão mascarada para exibição.
/// </summary>
public sealed class ProtecaoCpf(byte[] chaveHmac)
{
    public string Hash(string cpf) =>
        Convert.ToHexString(HMACSHA256.HashData(chaveHmac, Encoding.UTF8.GetBytes(Conversor.SomenteDigitos(cpf))));

    public static string Mascarar(string cpf)
    {
        var d = Conversor.SomenteDigitos(cpf);
        return d.Length == 11 ? $"***.{d[3..6]}.{d[6..9]}-**" : "***";
    }
}
