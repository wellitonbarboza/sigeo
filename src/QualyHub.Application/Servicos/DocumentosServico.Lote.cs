using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;

namespace QualyHub.Application.Servicos;

/// <summary>Código, título e revisão lidos do conteúdo (ou do nome) de um arquivo do SGQ.</summary>
public sealed record IdentificacaoArquivoSgq(string Arquivo, string? Codigo, string? Titulo, int? Revisao, string? DescricaoRevisao, string? Fonte);

/// <summary>Arquivo enviado na importação em lote, já identificado.</summary>
public sealed record ArquivoLoteSgq(string Nome, byte[] Conteudo, IdentificacaoArquivoSgq Identificacao)
{
    public bool Pdf => Path.GetExtension(Nome).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
}

public enum AcaoLoteSgq { Criar = 1, NovaRevisao = 2, AnexarNaRevisao = 3, Ignorar = 4 }

public sealed record ItemLoteSgq(string Codigo, string Titulo, string? Tipo, int Revisao, string? Editavel, string? Pdf, AcaoLoteSgq Acao, string Observacao, string? ServicoVinculado);

public sealed record ResumoLoteSgq(IReadOnlyList<ItemLoteSgq> Itens, IReadOnlyList<string> SemCodigo)
{
    public int Criar => Itens.Count(i => i.Acao == AcaoLoteSgq.Criar);
    public int Revisoes => Itens.Count(i => i.Acao is AcaoLoteSgq.NovaRevisao or AcaoLoteSgq.AnexarNaRevisao);
    public int Ignorados => Itens.Count(i => i.Acao == AcaoLoteSgq.Ignorar);
}

/// <summary>
/// Importação em lote de documentos já elaborados (procedimentos/ITS em .docx, fichas em .xlsx, com o PDF opcional).
/// Cada código vira um documento com a revisão do arquivo (REV01 → R01) em elaboração, com o editável e o PDF anexados:
/// a análise crítica, a aprovação e a publicação seguem o fluxo normal (assinatura eletrônica). Nada entra vigente sem aprovação.
/// Serviços controlados com o mesmo código passam a apontar para o documento (ITS do serviço).
/// </summary>
public sealed partial class DocumentosServico
{
    private static string Chave(string? codigo) => Regex.Replace((codigo ?? "").ToUpperInvariant(), "[^0-9A-Z]", "");

    /// <summary>Tipo do documento pelo prefixo do código, entre os tipos cadastrados (PEIS/ITS → ITS; FVS/FVM → ficha; FORM/F → formulário…).</summary>
    public static string? TipoPorCodigo(string? codigo, IReadOnlyList<string> tipos)
    {
        var prefixo = Regex.Match((codigo ?? "").ToUpperInvariant(), "^[A-Z]+").Value;
        var alvo = prefixo switch
        {
            "PEIS" or "ITS" or "PES" or "IT" => "ITS",
            "FVS" or "FVM" => "FVS",
            "FORM" or "F" => "F",
            "MQ" => "MQ", "PQO" => "PQO", "DT" => "DT", "DS" => "DS",
            _ => null
        };
        return alvo is null ? null : tipos.FirstOrDefault(t => Regex.IsMatch(t, $@"^{alvo}\b", RegexOptions.IgnoreCase));
    }

    public Task<ResumoLoteSgq> AnalisarLoteAsync(Guid pastaId, string? tipo, IReadOnlyList<string> tipos, IReadOnlyList<ArquivoLoteSgq> arquivos, CancellationToken ct = default) =>
        ProcessarLoteAsync(pastaId, tipo, tipos, arquivos, false, ct);

    public Task<ResumoLoteSgq> ImportarLoteAsync(Guid pastaId, string? tipo, IReadOnlyList<string> tipos, IReadOnlyList<ArquivoLoteSgq> arquivos, CancellationToken ct = default) =>
        ProcessarLoteAsync(pastaId, tipo, tipos, arquivos, true, ct);

    private async Task<ResumoLoteSgq> ProcessarLoteAsync(Guid pastaId, string? tipo, IReadOnlyList<string> tipos, IReadOnlyList<ArquivoLoteSgq> arquivos, bool aplicar, CancellationToken ct)
    {
        if (pastaId == Guid.Empty) throw new ValidacaoException(["Escolha a pasta (processo/área) dos documentos."]);
        if (arquivos.Count == 0) throw new ValidacaoException(["Selecione os arquivos."]);
        await using var db = fabrica.Criar();
        if (!await db.Pastas.AnyAsync(p => p.Id == pastaId, ct)) throw new ValidacaoException(["Pasta não encontrada."]);
        var docs = await db.Documentos.Include(d => d.Revisoes).ThenInclude(r => r.Etapas).ToListAsync(ct);
        var porChave = docs.GroupBy(d => Chave(d.Codigo)).ToDictionary(g => g.Key, g => g.First());
        var servicos = await db.ServicosControlados.Where(s => s.Ativo).ToListAsync(ct);
        arquivos = ParearPdfs(arquivos);
        var semCodigo = arquivos.Where(a => string.IsNullOrWhiteSpace(a.Identificacao.Codigo)).Select(a => a.Nome).ToList();
        var periodicidade = (await ParametrosServico.CarregarAsync(db, ct)).Int(Domain.Estrutura.Parametros.PeriodicidadeAnaliseCriticaMeses);

        var itens = new List<ItemLoteSgq>();
        foreach (var g in arquivos.Where(a => !string.IsNullOrWhiteSpace(a.Identificacao.Codigo)).GroupBy(a => Chave(a.Identificacao.Codigo)).OrderBy(g => g.Key))
        {
            var editavel = g.FirstOrDefault(a => !a.Pdf);
            var pdf = g.FirstOrDefault(a => a.Pdf);
            var id = (editavel ?? pdf)!.Identificacao;
            var codigo = id.Codigo!.Trim();
            var titulo = (editavel?.Identificacao.Titulo ?? pdf?.Identificacao.Titulo ?? codigo).Trim();
            var numero = editavel?.Identificacao.Revisao ?? pdf?.Identificacao.Revisao ?? 0;
            var tipoDoc = string.IsNullOrWhiteSpace(tipo) ? TipoPorCodigo(codigo, tipos) : tipo;
            var srv = servicos.FirstOrDefault(s => Chave(s.Codigo) == Chave(codigo));
            var vinculo = srv is null ? null : $"{srv.Codigo} – {srv.Nome}";
            var obs = new List<string>();
            if (g.Count(a => !a.Pdf) > 1) obs.Add($"{g.Count(a => !a.Pdf)} editáveis com o mesmo código: usado {editavel!.Nome}");
            if (g.Count(a => a.Pdf) > 1) obs.Add($"{g.Count(a => a.Pdf)} PDFs com o mesmo código: usado {pdf!.Nome}");

            ItemLoteSgq Item(AcaoLoteSgq acao, string motivo) =>
                new(codigo, titulo, tipoDoc, numero, editavel?.Nome, pdf?.Nome, acao, string.Join(" · ", obs.Prepend(motivo).Where(x => x.Length > 0)), vinculo);

            if (tipoDoc is null) { itens.Add(Item(AcaoLoteSgq.Ignorar, "Tipo não identificado pelo código: escolha o tipo do lote.")); continue; }

            if (!porChave.TryGetValue(Chave(codigo), out var doc))
            {
                itens.Add(Item(AcaoLoteSgq.Criar, $"Novo documento, R{numero:00} em elaboração."));
                if (!aplicar) continue;
                doc = new Documento
                {
                    Codigo = codigo, Titulo = titulo, Tipo = tipoDoc, PastaId = pastaId, ResponsavelUsuarioId = usuario.UsuarioId, ResponsavelNome = usuario.Nome,
                    PeriodicidadeMeses = periodicidade
                };
                db.Documentos.Add(doc);
                var rev = doc.NovaRevisao(MotivoRevisao.EmissaoInicial, null, usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora);
                rev.Revisao = numero;
                if (id.DescricaoRevisao is { Length: > 0 } dr) rev.DescricaoAlteracao = dr;
                rev.OrigemRevisao = "Importação em lote";
                porChave[Chave(codigo)] = doc;
                Log(db, EventoDocumentoTipo.Criacao, doc.Id, rev.Id, detalhe: $"{doc.Codigo} – {doc.Titulo} (importação em lote, {rev.Rotulo})");
                await AnexarLoteAsync(db, doc, rev, editavel, pdf, ct);
                Vincular(srv, doc);
                continue;
            }

            var andamento = doc.Revisoes.FirstOrDefault(r => r.Status is < StatusRevisao.Vigente and not StatusRevisao.Recusada);
            var maior = doc.Revisoes.Where(r => r.Status is not (StatusRevisao.Recusada or StatusRevisao.Cancelada)).Select(r => (int?)r.Revisao).Max();
            if (andamento is not null)
            {
                if (andamento.Revisao == numero && (andamento.ArquivoEditavelBlob is null || (pdf is not null && andamento.ArquivoPublicadoBlob is null)))
                {
                    itens.Add(Item(AcaoLoteSgq.AnexarNaRevisao, $"Anexa os arquivos na {andamento.Rotulo} em andamento."));
                    if (aplicar) { await AnexarLoteAsync(db, doc, andamento, andamento.ArquivoEditavelBlob is null ? editavel : null, andamento.ArquivoPublicadoBlob is null ? pdf : null, ct); Vincular(srv, doc); }
                }
                else itens.Add(Item(AcaoLoteSgq.Ignorar, $"Já existe a {andamento.Rotulo} em andamento."));
                continue;
            }
            if (maior is { } m && numero <= m)
            {
                itens.Add(Item(AcaoLoteSgq.Ignorar, numero == m ? $"R{numero:00} já cadastrada." : $"O sistema já tem a R{m:00} (mais nova que a do arquivo)."));
                if (aplicar) Vincular(srv, doc);
                continue;
            }
            itens.Add(Item(AcaoLoteSgq.NovaRevisao, $"Abre a R{numero:00} em elaboração (vigente continua valendo até publicar)."));
            if (!aplicar) continue;
            var nova = doc.NovaRevisao(MotivoRevisao.Melhoria, id.DescricaoRevisao is { Length: > 0 } d ? d : $"Revisão R{numero:00} importada do arquivo {(editavel ?? pdf)!.Nome}",
                usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora);
            nova.Revisao = numero;
            nova.OrigemRevisao = "Importação em lote";
            db.Revisoes.Add(nova);
            Log(db, EventoDocumentoTipo.NovaRevisao, doc.Id, nova.Id, detalhe: $"{nova.Rotulo}: importação em lote");
            await AnexarLoteAsync(db, doc, nova, editavel, pdf, ct);
            Vincular(srv, doc);
        }
        if (aplicar) await db.SaveChangesAsync(ct);
        return new ResumoLoteSgq(itens, semCodigo);
    }

    /// <summary>
    /// PDF com o mesmo nome do editável (ex.: PEIS.01-Forma.REV01.docx e .pdf) assume a identificação lida do editável —
    /// o nome do arquivo pode ter outra numeração que o conteúdo.
    /// </summary>
    public static IReadOnlyList<ArquivoLoteSgq> ParearPdfs(IReadOnlyList<ArquivoLoteSgq> arquivos)
    {
        var editaveis = arquivos.Where(a => !a.Pdf).GroupBy(a => Path.GetFileNameWithoutExtension(a.Nome), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        return arquivos.Select(a => a.Pdf && editaveis.TryGetValue(Path.GetFileNameWithoutExtension(a.Nome), out var e)
            ? a with { Identificacao = e.Identificacao with { Arquivo = a.Nome, DescricaoRevisao = null } } : a).ToList();
    }

    private static void Vincular(Domain.Cadastros.ServicoControlado? srv, Documento doc)
    {
        if (srv is not null && srv.DocumentoItsId is null) srv.DocumentoItsId = doc.Id;
    }

    private async Task AnexarLoteAsync(IQualyHubDb db, Documento doc, DocumentoRevisao rev, ArquivoLoteSgq? editavel, ArquivoLoteSgq? pdf, CancellationToken ct)
    {
        if (editavel is not null)
        {
            using var s = new MemoryStream(editavel.Conteudo);
            var salvo = await armazenamento.SalvarAsync(s, editavel.Nome, "sgq", null, ct);
            rev.ArquivoEditavelBlob = salvo.Blob;
            rev.ArquivoEditavelNome = Documento.NomeArquivo(doc.Codigo, doc.Titulo, rev.Revisao, Path.GetExtension(editavel.Nome));
            Log(db, EventoDocumentoTipo.ArquivoMovido, doc.Id, rev.Id, detalhe: $"Editável: {rev.ArquivoEditavelNome} (sha256 {salvo.Hash[..12]}…)");
        }
        if (pdf is not null)
        {
            using var s = new MemoryStream(pdf.Conteudo);
            var salvo = await armazenamento.SalvarAsync(s, pdf.Nome, "sgq", "application/pdf", ct);
            rev.ArquivoPublicadoBlob = salvo.Blob;
            rev.ArquivoPublicadoNome = Documento.NomeArquivo(doc.Codigo, doc.Titulo, rev.Revisao, "pdf");
            rev.HashSha256 = salvo.Hash;
            Log(db, EventoDocumentoTipo.ArquivoMovido, doc.Id, rev.Id, detalhe: $"Publicado: {rev.ArquivoPublicadoNome} (sha256 {salvo.Hash[..12]}…)");
        }
    }
}
