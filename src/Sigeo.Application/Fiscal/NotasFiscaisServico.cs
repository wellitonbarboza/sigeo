using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

/// <summary>
/// Notas fiscais de entrada: importa o XML (arquivo ou SIEG), liga ao fornecedor e ao pedido pela regra
/// (CNPJ do emitente, nº do pedido citado na nota, descrição dos itens) e deixa pronta para gerar o recebimento.
/// Só aceita nota destinada a um CNPJ da própria construtora (estabelecimentos e SPEs).
/// </summary>
public sealed class NotasFiscaisServico(IFabricaDb fabrica, IArmazenamento armazenamento, TimeProvider relogio)
{
    public static readonly StatusPedido[] PedidosAbertos = [StatusPedido.EnviadoFornecedor, StatusPedido.EntregueParcial, StatusPedido.Aprovado];

    /// <summary>CNPJs (só dígitos) que recebem nota na empresa: estabelecimentos e SPEs.</summary>
    public async Task<HashSet<string>> CnpjsDaEmpresaAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var est = await db.Estabelecimentos.AsNoTracking().Select(e => e.Cnpj).ToListAsync(ct);
        var spes = await db.Spes.IgnoreQueryFilters([FiltrosNomes.Obra]).AsNoTracking().Select(s => s.Cnpj).ToListAsync(ct);
        return est.Concat(spes).Select(Conversor.SomenteDigitos).Where(c => c.Length == 14).ToHashSet();
    }

    public async Task<ResultadoImportacaoNotas> ImportarArquivoAsync(Stream conteudo, string nomeArquivo, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await conteudo.CopyToAsync(ms, ct);
        ms.Position = 0;
        List<XmlLido> lidos;
        try { lidos = LeitorNfe.Ler(ms, nomeArquivo); }
        catch (InvalidDataException) { throw new RegraDeNegocioException("Arquivo ZIP inválido."); }
        return await ImportarAsync(lidos, OrigemNota.Manual, null, null, null, ct);
    }

    /// <summary>Grava as notas lidas (sem repetir chave), aplica cancelamentos, guarda o XML e tenta o vínculo automático.</summary>
    public async Task<ResultadoImportacaoNotas> ImportarAsync(IReadOnlyList<XmlLido> lidos, OrigemNota origem, string? cnpj, DateOnly? de, DateOnly? ate, CancellationToken ct = default)
    {
        var cnpjs = await CnpjsDaEmpresaAsync(ct);
        var avisos = new List<string>();
        int novas = 0, repetidas = 0, canceladas = 0, vinculadas = 0;
        await using var db = fabrica.Criar();
        var chaves = lidos.OfType<NotaLida>().Select(n => n.Nota.Chave).Concat(lidos.OfType<CancelamentoLido>().Select(c => c.Chave)).Distinct().ToList();
        var existentes = await db.NotasFiscais.Where(n => chaves.Contains(n.Chave)).ToDictionaryAsync(n => n.Chave, ct);
        var novasNotas = new List<NotaFiscalEntrada>();
        foreach (var l in lidos)
        {
            switch (l)
            {
                case XmlIgnorado i: avisos.Add($"{i.Arquivo}: {i.Motivo}"); break;
                case NotaLida nl when existentes.ContainsKey(nl.Nota.Chave) || novasNotas.Any(x => x.Chave == nl.Nota.Chave): repetidas++; break;
                case NotaLida nl when cnpjs.Count > 0 && !cnpjs.Contains(nl.Nota.DestinatarioCnpj):
                    avisos.Add($"NF {nl.Nota.Numero} ({nl.Nota.EmitenteNome}): destinada a outro CNPJ ({Conversor.SomenteDigitos(nl.Nota.DestinatarioCnpj)}), não importada.");
                    break;
                case NotaLida nl:
                    var n = nl.Nota;
                    n.Origem = origem;
                    using (var xml = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(nl.Xml)))
                        n.ArquivoXml = (await armazenamento.SalvarAsync(xml, $"{n.Chave}.xml", "notas-fiscais", "application/xml", ct)).Blob;
                    db.NotasFiscais.Add(n);
                    novasNotas.Add(n);
                    novas++;
                    break;
                case CancelamentoLido c:
                    if (existentes.TryGetValue(c.Chave, out var e) && e.Situacao != SituacaoNota.Cancelada)
                    {
                        if (e.Situacao == SituacaoNota.Recebimento) avisos.Add($"NF {e.Numero} ({e.EmitenteNome}) foi CANCELADA pelo fornecedor, mas já tem recebimento: confira com o fiscal.");
                        e.Situacao = SituacaoNota.Cancelada; e.VinculoDetalhe = "Cancelada pelo emitente (evento SEFAZ).";
                        canceladas++;
                    }
                    break;
            }
        }
        await db.SaveChangesAsync(ct);
        foreach (var n in novasNotas)
            if (await VincularAutomaticoAsync(db, n, ct)) vinculadas++;
        db.ImportacoesNota.Add(new ImportacaoNota
        {
            Origem = origem, Cnpj = cnpj, De = de, Ate = ate, Lidas = lidos.Count, Novas = novas, Canceladas = canceladas,
            Detalhe = avisos.Count == 0 ? null : Limitar(string.Join("\n", avisos.Take(20)), 2000)
        });
        await db.SaveChangesAsync(ct);
        return new(lidos.Count, novas, repetidas, canceladas, vinculadas, avisos);
    }

    /// <summary>
    /// Regra sem IA: fornecedor pelo CNPJ do emitente; pedido pelo nº citado na nota entre os pedidos abertos do fornecedor
    /// (ou o único aberto); itens pela descrição/código/unidade. Retorna se vinculou ao pedido.
    /// </summary>
    internal static async Task<bool> VincularAutomaticoAsync(ISigeoDb db, NotaFiscalEntrada n, CancellationToken ct)
    {
        if (!n.PodeVincular) return false;
        var forns = await db.Fornecedores.AsNoTracking().Select(f => new { f.Id, f.CpfCnpj }).ToListAsync(ct);
        var forn = forns.FirstOrDefault(f => Conversor.SomenteDigitos(f.CpfCnpj) == n.EmitenteCnpj);
        if (forn is null) { n.VinculoDetalhe = "Fornecedor não cadastrado (CNPJ do emitente)."; await db.SaveChangesAsync(ct); return false; }
        n.FornecedorId = forn.Id;
        // A nota não é da obra: procura o pedido em todas as obras da empresa.
        var abertos = await db.Pedidos.IgnoreQueryFilters([FiltrosNomes.Obra]).AsNoTracking()
            .Where(p => p.FornecedorId == forn.Id && PedidosAbertos.Contains(p.Status)).Select(p => new { p.Id, p.Numero, p.ObraId }).ToListAsync(ct);
        var citados = LeitorNfe.PedidosCitados(n);
        var porNumero = abertos.Where(p => citados.Contains(p.Numero)).ToList();
        var escolhido = porNumero.Count == 1 ? porNumero[0] : porNumero.Count == 0 && abertos.Count == 1 ? abertos[0] : null;
        if (escolhido is null)
        {
            n.VinculoDetalhe = abertos.Count == 0 ? "Fornecedor sem pedido aberto." : porNumero.Count > 1 ? "A nota cita mais de um pedido: escolha na tela."
                : $"Fornecedor com {abertos.Count} pedidos abertos e a nota não cita o número: escolha na tela (ou use a análise por IA).";
            await db.SaveChangesAsync(ct);
            return false;
        }
        n.VincularPedido(escolhido.Id, escolhido.ObraId, OrigemVinculo.Regra,
            porNumero.Count == 1 ? $"Pedido {escolhido.Numero} citado na nota." : $"Único pedido aberto do fornecedor (nº {escolhido.Numero}): confira.");
        await PararItensAsync(db, n, OrigemVinculo.Regra, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task PararItensAsync(ISigeoDb db, NotaFiscalEntrada n, OrigemVinculo origem, CancellationToken ct)
    {
        var itensPed = await db.PedidoItens.AsNoTracking().Where(i => i.PedidoId == n.PedidoId)
            .Select(i => new ItemPedidoParaConferencia(i.Id, i.Descricao, i.Unidade, i.Quantidade - i.QuantidadeRecebidaAceita, i.CodigoInsumo)).ToListAsync(ct);
        var pares = ConferenciaNota.Parear(n.Itens.ToList(), itensPed);
        foreach (var i in n.Itens)
        {
            if (pares.TryGetValue(i.Item, out var p)) { i.PedidoItemId = p.PedidoItemId; i.Confianca = p.Confianca; i.VinculoOrigem = origem; }
            else { i.PedidoItemId = null; i.Confianca = null; i.VinculoOrigem = null; }
        }
    }

    /// <summary>Vínculo escolhido pela pessoa (ou sugestão da IA aceita): pedido do mesmo fornecedor, itens pareados de novo.</summary>
    public async Task VincularPedidoAsync(Guid notaId, Guid pedidoId, OrigemVinculo origem = OrigemVinculo.Manual, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var n = await db.NotasFiscais.Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
        var p = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pedidoId, ct) ?? throw new RegraDeNegocioException("Pedido não encontrado ou sem acesso à obra.");
        if (n.FornecedorId is { } f && p.FornecedorId != f) throw new RegraDeNegocioException("O pedido é de outro fornecedor.");
        if (!n.PodeVincular) throw new RegraDeNegocioException("Nota já recebida, cancelada ou ignorada.");
        n.FornecedorId = p.FornecedorId;
        n.VincularPedido(p.Id, p.ObraId, origem, origem == OrigemVinculo.Ia ? $"Pedido {p.Numero} sugerido pela IA e aceito." : $"Pedido {p.Numero} escolhido na tela.");
        await PararItensAsync(db, n, origem, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DefinirItemAsync(Guid notaId, int item, Guid? pedidoItemId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var n = await db.NotasFiscais.Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
        if (!n.PodeVincular || n.PedidoId is null) throw new RegraDeNegocioException("Vincule a nota a um pedido antes de ajustar os itens.");
        var i = n.Itens.FirstOrDefault(x => x.Item == item) ?? throw new RegraDeNegocioException("Item da nota não encontrado.");
        if (pedidoItemId is { } pi && !await db.PedidoItens.AnyAsync(x => x.Id == pi && x.PedidoId == n.PedidoId, ct))
            throw new RegraDeNegocioException("O item escolhido não é do pedido vinculado.");
        i.PedidoItemId = pedidoItemId; i.VinculoOrigem = pedidoItemId is null ? null : OrigemVinculo.Manual; i.Confianca = pedidoItemId is null ? null : 1;
        await db.SaveChangesAsync(ct);
    }

    public async Task DesvincularAsync(Guid notaId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var n = await db.NotasFiscais.Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
        if (!n.PodeVincular) throw new RegraDeNegocioException("Nota já recebida, cancelada ou ignorada.");
        n.Desvincular("Desvinculada na tela.");
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Nota que não gera recebimento (serviço, devolução, uso e consumo fora dos pedidos): sai da fila.</summary>
    public async Task IgnorarAsync(Guid notaId, string motivo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo.");
        await using var db = fabrica.Criar();
        var n = await db.NotasFiscais.FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
        if (n.Situacao == SituacaoNota.Recebimento) throw new RegraDeNegocioException("Nota com recebimento registrado não pode ser ignorada.");
        n.Situacao = SituacaoNota.Ignorada; n.VinculoDetalhe = $"Ignorada: {motivo.Trim()}";
        await db.SaveChangesAsync(ct);
    }

    public async Task VoltarParaFilaAsync(Guid notaId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var n = await db.NotasFiscais.FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
        if (n.Situacao != SituacaoNota.Ignorada) throw new RegraDeNegocioException("Só nota ignorada volta para a fila.");
        n.Situacao = n.PedidoId is null ? SituacaoNota.Recebida : SituacaoNota.Vinculada; n.VinculoDetalhe = "Voltou para a fila.";
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Recebimento registrado com a chave da nota: a nota sai da fila (chamado pelo serviço de recebimentos).</summary>
    public static async Task MarcarRecebidaAsync(ISigeoDb db, Guid recebimentoId, string? chave, CancellationToken ct)
    {
        var c = Conversor.SomenteDigitos(chave);
        if (c.Length != 44) return;
        var n = await db.NotasFiscais.FirstOrDefaultAsync(x => x.Chave == c, ct);
        if (n is null || n.Situacao is SituacaoNota.Cancelada) return;
        n.Situacao = SituacaoNota.Recebimento; n.RecebimentoId = recebimentoId;
    }

    /// <summary>Busca no SIEG as notas dos CNPJs da empresa no período (respeita o limite de 2 meses por chamada).</summary>
    public async Task<ResultadoImportacaoNotas> BuscarNaFonteAsync(IFonteNotas fonte, Guid empresaId, DateOnly de, DateOnly ate, CancellationToken ct = default)
    {
        if (!fonte.Configurada(empresaId)) throw new RegraDeNegocioException("Integração SIEG não configurada para esta empresa.");
        if (ate < de) (de, ate) = (ate, de);
        if (ate.DayNumber - de.DayNumber > 62) throw new RegraDeNegocioException("Período máximo de 2 meses por busca.");
        int lidas = 0, novas = 0, rep = 0, canc = 0, vinc = 0;
        var avisos = new List<string>();
        foreach (var cnpj in await CnpjsDaEmpresaAsync(ct))
        {
            try
            {
                var xmls = await fonte.BaixarAsync(empresaId, cnpj, de, ate, ct);
                var lidos = xmls.Select(x => LeitorNfe.LerTexto(x.Xml, x.Nome)).ToList();
                var r = await ImportarAsync(lidos, OrigemNota.Sieg, cnpj, de, ate, ct);
                lidas += r.Lidas; novas += r.Novas; rep += r.Repetidas; canc += r.Canceladas; vinc += r.Vinculadas; avisos.AddRange(r.Avisos);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                avisos.Add($"CNPJ {cnpj}: {e.Message}");
                await using var db = fabrica.Criar();
                db.ImportacoesNota.Add(new ImportacaoNota { Origem = OrigemNota.Sieg, Cnpj = cnpj, De = de, Ate = ate, Falhou = true, Detalhe = Limitar(e.Message, 2000) });
                await db.SaveChangesAsync(ct);
            }
        }
        return new(lidas, novas, rep, canc, vinc, avisos);
    }

    private static string Limitar(string s, int max) => s.Length <= max ? s : s[..max];

    public DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
}
