using Microsoft.EntityFrameworkCore;
using Groundkey.Application.Abstracoes;
using Groundkey.Application.Comum;
using Groundkey.Application.IA;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Fiscal;

namespace Groundkey.Application.Fiscal;

/// <summary>Leitura de imagem sem custo de IA: códigos de barras/QR e redução para envio (implementação com ZXing + SkiaSharp).</summary>
public interface ILeitorImagemNota
{
    /// <summary>Textos dos códigos de barras (Code 128 do DANFE) e QR Codes encontrados na foto; vazio se nenhum.</summary>
    IReadOnlyList<string> LerCodigos(byte[] imagem);
    /// <summary>JPEG com o maior lado limitado (o Document Intelligence gratuito aceita até 4 MB por arquivo).</summary>
    byte[] Reduzir(byte[] imagem, int maiorLado = 2000, int qualidade = 85);
}

public sealed record ItemNotaLido(string Descricao, string? Unidade, decimal Quantidade, decimal? ValorUnitario);
public sealed record PedidoDaNota(Guid Id, int Numero, string Obra);

/// <summary>Resultado da leitura da nota no recebimento: o que foi lido, o que já existe no sistema e avisos para conferir.</summary>
public sealed record NotaLidaNoRecebimento(
    string Chave, string? Numero, string? Serie, string? CnpjEmitente, string? Emitente,
    Guid? FornecedorId, string? Fornecedor,
    Guid? NotaId, SituacaoNota? Situacao, Guid? PedidoDaNotaId,
    IReadOnlyList<PedidoDaNota> PedidosAbertos, int? RecebimentoExistente,
    IReadOnlyList<ItemNotaLido> Itens, string Origem, IReadOnlyList<string> Avisos)
{
    /// <summary>Nota importada (XML/SIEG) já ligada ao pedido: o recebimento sai inteiro dela.</summary>
    public bool ProntaParaReceber => NotaId is not null && PedidoDaNotaId is not null && Situacao == SituacaoNota.Vinculada;
}

/// <summary>
/// Leitura da NF-e no recebimento pela câmera do tablet/celular:
/// 1. código de barras do DANFE (chave de 44 dígitos, sem custo);
/// 2. se o código não sair, por botão, leitura da foto pela IA (Document Intelligence; o modelo só entra quando a nota não foi importada,
///    para ler os itens). A chave só é aceita com DV correto.
/// Nada é gravado aqui: a tela preenche o recebimento para a pessoa conferir.
/// </summary>
public sealed class LeituraNotaServico(IFabricaDb fabrica, ILeitorImagemNota imagem, AnalisesIaServico ia, ILeitorDocumentos documentos, IAssistenteIa assistente)
{
    public const long LimiteFotoBytes = 20 * 1024 * 1024;

    /// <summary>IA de leitura de documento ligada (mostra o botão "Ler com IA").</summary>
    public bool IaDisponivel => documentos.Configurado && assistente.Configurado;

    /// <summary>Lê o código de barras da foto. Null quando não achou uma chave válida (a tela oferece a leitura por IA).</summary>
    public async Task<NotaLidaNoRecebimento?> LerCodigoAsync(byte[] foto, CancellationToken ct = default)
    {
        var codigos = await Task.Run(() => imagem.LerCodigos(foto), ct);
        var chave = codigos.Select(ChaveNfe.Encontrar).FirstOrDefault(c => c is not null);
        return chave is null ? null : await ResolverAsync(chave, "código de barras", [], null, ct);
    }

    /// <summary>Leitura da foto pela IA (conta no limite mensal). Primeiro o texto (OCR); o modelo só lê os itens se a nota não estiver importada.</summary>
    public async Task<NotaLidaNoRecebimento> LerComIaAsync(byte[] foto, Guid? referenciaId, CancellationToken ct = default)
    {
        var jpeg = await Task.Run(() => imagem.Reduzir(foto), ct);
        string texto;
        using (var ms = new MemoryStream(jpeg)) texto = (await ia.LerTextoAsync(ms, "image/jpeg", "danfe-leitura", referenciaId, ct)).Texto;

        var chave = ChaveNfe.Encontrar(texto);
        if (chave is not null)
        {
            var r = await ResolverAsync(chave, "IA (texto da foto)", [], null, ct);
            if (r.NotaId is not null) return r;
        }
        var d = await ia.ExtrairDanfeAsync(texto, referenciaId, ct);
        chave ??= ChaveNfe.Encontrar(d.Chave);
        if (chave is null)
            throw new RegraDeNegocioException("Não foi possível achar a chave de acesso na foto. Fotografe o DANFE inteiro, com luz, ou digite a chave.");
        var itens = (d.Itens ?? []).Where(i => !string.IsNullOrWhiteSpace(i.Descricao) && i.Quantidade > 0)
            .Select(i => new ItemNotaLido(i.Descricao!.Trim(), i.Unidade, i.Quantidade, i.ValorUnitario)).ToList();
        return await ResolverAsync(chave, "IA (foto do DANFE)", itens, d.Emitente, ct);
    }

    /// <summary>Chave digitada, colada ou lida por leitor de código de barras USB/Bluetooth.</summary>
    public Task<NotaLidaNoRecebimento> ResolverChaveAsync(string chave, CancellationToken ct = default) =>
        ChaveNfe.Valida(chave) ? ResolverAsync(ChaveNfe.SoDigitos(chave), "chave digitada", [], null, ct)
            : throw new RegraDeNegocioException("Chave da NF-e inválida (44 dígitos com dígito verificador correto).");

    private async Task<NotaLidaNoRecebimento> ResolverAsync(string chave, string origem, IReadOnlyList<ItemNotaLido> itens, string? emitenteLido, CancellationToken ct)
    {
        var partes = ChaveNfe.Partes(chave)!;
        var avisos = new List<string>();
        await using var db = fabrica.CriarLeitura();

        var nota = await db.NotasFiscais.AsNoTracking().Where(n => n.Chave == chave)
            .Select(n => new { n.Id, n.Numero, n.Serie, n.EmitenteCnpj, n.EmitenteNome, n.Situacao, n.PedidoId, n.FornecedorId }).FirstOrDefaultAsync(ct);
        // Duplicidade vale para a empresa toda (mesma nota recebida em outra obra).
        var existente = await db.Recebimentos.IgnoreQueryFilters([FiltrosNomes.Obra]).AsNoTracking()
            .Where(r => r.ChaveNFe == chave).Select(r => (int?)r.Numero).FirstOrDefaultAsync(ct);
        if (existente is { } rn) avisos.Add($"Esta nota já foi recebida no recebimento {rn}.");

        var cnpj = nota?.EmitenteCnpj ?? partes.CnpjEmitente;
        var forns = await db.Fornecedores.AsNoTracking().Select(f => new { f.Id, f.CpfCnpj, f.RazaoSocial, f.NomeFantasia }).ToListAsync(ct);
        var forn = nota?.FornecedorId is { } fid ? forns.FirstOrDefault(f => f.Id == fid) : forns.FirstOrDefault(f => Conversor.SomenteDigitos(f.CpfCnpj) == cnpj);
        if (forn is null) avisos.Add($"Fornecedor do CNPJ {Conversor.FormatarCnpj(cnpj)} não está cadastrado.");

        List<PedidoDaNota> pedidos = [];
        if (forn is not null)
        {
            var ps = await db.Pedidos.AsNoTracking()
                .Where(p => p.FornecedorId == forn.Id && (p.Status == StatusPedido.EnviadoFornecedor || p.Status == StatusPedido.EntregueParcial))
                .OrderByDescending(p => p.Numero).Select(p => new { p.Id, p.Numero, p.ObraId }).Take(30).ToListAsync(ct);
            var obras = await db.Obras.AsNoTracking().Select(o => new { o.Id, o.Nome }).ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
            pedidos = ps.Select(p => new PedidoDaNota(p.Id, p.Numero, obras.GetValueOrDefault(p.ObraId, ""))).ToList();
            if (pedidos.Count == 0) avisos.Add("O fornecedor não tem pedido aguardando entrega nas suas obras.");
        }

        if (nota is null) avisos.Add("Nota ainda não importada (XML/SIEG): os itens não vêm da nota — confira as quantidades.");
        else if (nota.Situacao == SituacaoNota.Cancelada) avisos.Add("Nota CANCELADA na SEFAZ: não receba o material com ela.");
        else if (nota.Situacao == SituacaoNota.Ignorada) avisos.Add("Nota marcada como ignorada em Notas fiscais.");
        else if (nota.Situacao == SituacaoNota.Recebida && nota.PedidoId is null) avisos.Add("Nota importada, mas ainda sem pedido vinculado: escolha o pedido.");

        return new NotaLidaNoRecebimento(chave, (nota?.Numero ?? partes.Numero).ToString(), nota?.Serie ?? partes.Serie, cnpj,
            nota?.EmitenteNome ?? emitenteLido, forn?.Id, forn is null ? null : string.IsNullOrWhiteSpace(forn.NomeFantasia) ? forn.RazaoSocial : forn.NomeFantasia,
            nota?.Id, nota?.Situacao, nota?.PedidoId, pedidos, existente, itens, origem, avisos);
    }
}
