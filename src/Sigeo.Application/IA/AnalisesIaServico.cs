using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Fiscal;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.IA;

public sealed record SugestaoRnc(string Resumo, IReadOnlyList<string> CausasProvaveis, IReadOnlyList<string> CincoPorques, string Disposicao,
    string AcaoCorretiva, string AcaoPreventiva, int PrazoDias, IReadOnlyList<string> Referencias);

public sealed record SugestaoProduto(string? Grupo, string? SubGrupo, string? Unidade, IReadOnlyList<string> PossiveisDuplicados, string? Observacao);

public sealed record ResultadoLaudo(string? Tipo, string? Emitente, string? Numero, DateOnly? DataEmissao, DateOnly? Validade, string? Lote, string? Material,
    IReadOnlyList<ResultadoEnsaioLido> Resultados, bool? Conforme, string? Conclusao, int Paginas);

public sealed record ResultadoEnsaioLido(string Ensaio, string? Valor, string? Unidade, string? Especificacao, bool? Conforme);

/// <summary>
/// Usos de IA do Sigeo. Toda análise é acionada por botão (ou pela rotina de notas), registrada em UsosIa
/// (limite mensal por construtora) e devolvida como sugestão para a pessoa conferir — a IA não aprova, não escolhe vencedor
/// e não grava nada fora do campo de análise, exceto o vínculo nota × pedido com confiança alta (marcado "sugestão IA").
/// </summary>
public sealed class AnalisesIaServico(IFabricaDb fabrica, IAssistenteIa ia, ILeitorDocumentos leitor, IUsuarioAtual usuario, TimeProvider relogio, LimitesIa limites)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString };
    private static readonly CultureInfo Br = new("pt-BR");
    public const decimal ConfiancaParaAplicar = 0.8m;

    private const string Base = "Você é assistente de engenharia e suprimentos de uma construtora brasileira (obras residenciais, PBQP-H/SiAC, NBR). " +
        "Responda em português do Brasil, objetivo, sem inventar dados que não estejam no conteúdo. Quando não houver informação suficiente, diga isso.";

    private async Task<T> PerguntarJsonAsync<T>(string funcao, Guid? referencia, string instrucoes, string conteudo, int maxSaida, CancellationToken ct)
    {
        await GarantirLimiteAsync(ct);
        RespostaIa r;
        try { r = await ia.PerguntarAsync(Base + " " + instrucoes, conteudo, json: true, maxSaida, ct); }
        catch (Exception e) when (e is not OperationCanceledException and not RegraDeNegocioException)
        {
            await RegistrarAsync(funcao, referencia, 0, 0, 0, falhou: true, ct);
            throw new RegraDeNegocioException("O serviço de IA não respondeu. Tente de novo em instantes.");
        }
        await RegistrarAsync(funcao, referencia, r.TokensEntrada, r.TokensSaida, 0, falhou: false, ct);
        try { return JsonSerializer.Deserialize<T>(r.Texto, Json) ?? throw new JsonException(); }
        catch (JsonException) { throw new RegraDeNegocioException("A IA respondeu fora do formato esperado. Tente de novo."); }
    }

    private async Task GarantirLimiteAsync(CancellationToken ct)
    {
        if (!ia.Configurado) throw new RegraDeNegocioException("IA não configurada (Azure OpenAI). Veja o script configurar-ia.sh.");
        var inicio = new DateTimeOffset(relogio.GetUtcNow().Year, relogio.GetUtcNow().Month, 1, 0, 0, 0, TimeSpan.Zero);
        await using var db = fabrica.CriarLeitura();
        var usadas = await db.UsosIa.CountAsync(u => u.CriadoEm >= inicio && !u.Falhou, ct);
        if (usadas >= limites.ChamadasPorMes)
            throw new RegraDeNegocioException($"Limite mensal de análises por IA atingido ({limites.ChamadasPorMes}). Ajuste em IA:LimiteMensalPorEmpresa.");
    }

    private async Task RegistrarAsync(string funcao, Guid? referencia, int entrada, int saida, int paginas, bool falhou, CancellationToken ct)
    {
        await using var db = fabrica.Criar();
        db.UsosIa.Add(new UsoIa { Funcao = funcao, ReferenciaId = referencia, TokensEntrada = entrada, TokensSaida = saida, Paginas = paginas, Falhou = falhou });
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Nota fiscal × pedido ─────────────

    private sealed record ItemIa(int Item, Guid? PedidoItemId, decimal Confianca, string? Observacao);
    private sealed record AnaliseNotaIa(int? Pedido, decimal Confianca, string? Justificativa, List<ItemIa>? Itens, List<string>? Alertas);

    /// <summary>
    /// Compara a nota com os pedidos abertos do fornecedor: sugere o pedido e o item de cada linha, e aponta divergências
    /// (preço, quantidade, unidade, item fora do pedido). Com confiança ≥ 80% e nota ainda sem pedido, já deixa vinculada como "sugestão IA".
    /// </summary>
    public async Task AnalisarNotaAsync(Guid notaId, CancellationToken ct = default)
    {
        NotaFiscalEntrada n;
        List<(Guid Id, int Numero, string Obra, List<(Guid Id, string Desc, string? Un, decimal Saldo, decimal Unit)> Itens)> pedidos = [];
        await using (var db = fabrica.CriarLeitura())
        {
            n = await db.NotasFiscais.AsNoTracking().Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == notaId, ct) ?? throw new RegraDeNegocioException("Nota não encontrada.");
            if (n.FornecedorId is null) throw new RegraDeNegocioException("Cadastre o fornecedor (CNPJ do emitente) antes da análise.");
            var ps = await db.Pedidos.AsNoTracking().Include(p => p.Itens)
                .Where(p => p.FornecedorId == n.FornecedorId && (NotasFiscaisServico.PedidosAbertos.Contains(p.Status) || p.Id == n.PedidoId))
                .OrderByDescending(p => p.Numero).Take(10).Select(p => new { p.Id, p.Numero, Obra = db.Obras.Where(o => o.Id == p.ObraId).Select(o => o.Nome).FirstOrDefault() ?? "", p.Itens }).ToListAsync(ct);
            pedidos = ps.Select(p => (p.Id, p.Numero, p.Obra, p.Itens.Select(i => (i.Id, i.Descricao, i.Unidade, i.Quantidade - i.QuantidadeRecebidaAceita, i.ValorUnitario)).ToList())).ToList();
        }
        if (pedidos.Count == 0) throw new RegraDeNegocioException("O fornecedor não tem pedido aberto para comparar.");

        var c = new StringBuilder();
        c.AppendLine($"NOTA FISCAL {n.Numero}/{n.Serie} emitida em {n.Emissao:dd/MM/yyyy} por {n.EmitenteNome}, valor {n.ValorTotal.ToString("N2", Br)}.");
        if (!string.IsNullOrWhiteSpace(n.InformacoesComplementares)) c.AppendLine($"Informações complementares: {Cortar(n.InformacoesComplementares, 800)}");
        c.AppendLine("Itens da nota (item | código | descrição | unidade | quantidade | valor unitário | pedido citado):");
        foreach (var i in n.Itens.OrderBy(i => i.Item))
            c.AppendLine($"{i.Item} | {i.Codigo} | {i.Descricao} | {i.Unidade} | {i.Quantidade.ToString("0.###", CultureInfo.InvariantCulture)} | {i.ValorUnitario.ToString("0.####", CultureInfo.InvariantCulture)} | {i.PedidoCitado}");
        c.AppendLine("PEDIDOS ABERTOS DO FORNECEDOR:");
        foreach (var p in pedidos)
        {
            c.AppendLine($"Pedido {p.Numero} (obra {p.Obra}) — itens (id | descrição | unidade | saldo a receber | valor unitário):");
            foreach (var i in p.Itens) c.AppendLine($"  {i.Id} | {i.Desc} | {i.Un} | {i.Saldo.ToString("0.###", CultureInfo.InvariantCulture)} | {i.Unit.ToString("0.####", CultureInfo.InvariantCulture)}");
        }
        const string instr = "Compare a nota fiscal com os pedidos. Escolha o pedido que a nota atende (ou null se nenhum) e, para cada item da nota, o id do item do pedido " +
            "correspondente (considere sinônimos, unidades de embalagem e abreviações). Liste alertas objetivos: diferença de preço unitário acima de 2%, quantidade acima do saldo, " +
            "unidade diferente, item sem correspondente no pedido. Responda só JSON: {\"pedido\": número|null, \"confianca\": 0 a 1, \"justificativa\": \"...\", " +
            "\"itens\": [{\"item\": n, \"pedidoItemId\": \"guid\"|null, \"confianca\": 0 a 1, \"observacao\": \"...\"}], \"alertas\": [\"...\"]}";
        var r = await PerguntarJsonAsync<AnaliseNotaIa>("nota-pedido", notaId, instr, c.ToString(), 1500, ct);

        var escolhido = r.Pedido is { } num ? pedidos.FirstOrDefault(p => p.Numero == num) : default;
        var texto = new StringBuilder();
        texto.AppendLine(escolhido.Id == Guid.Empty ? "Pedido sugerido: nenhum." : $"Pedido sugerido: {escolhido.Numero} (confiança {r.Confianca:P0}).");
        if (!string.IsNullOrWhiteSpace(r.Justificativa)) texto.AppendLine(r.Justificativa);
        foreach (var a in r.Alertas ?? []) texto.AppendLine($"⚠ {a}");
        foreach (var i in (r.Itens ?? []).Where(i => !string.IsNullOrWhiteSpace(i.Observacao))) texto.AppendLine($"Item {i.Item}: {i.Observacao}");

        await using var dbw = fabrica.Criar();
        var nota = await dbw.NotasFiscais.Include(x => x.Itens).FirstAsync(x => x.Id == notaId, ct);
        nota.AnaliseIa = Cortar(texto.ToString(), 8000);
        if (nota.PodeVincular && escolhido.Id != Guid.Empty && r.Confianca >= ConfiancaParaAplicar)
        {
            var obra = await dbw.Pedidos.Where(p => p.Id == escolhido.Id).Select(p => p.ObraId).FirstAsync(ct);
            if (nota.PedidoId != escolhido.Id) nota.VincularPedido(escolhido.Id, obra, OrigemVinculo.Ia, $"Pedido {escolhido.Numero} sugerido pela IA ({r.Confianca:P0}): confira os itens.");
            var validos = escolhido.Itens.Select(i => i.Id).ToHashSet();
            foreach (var s in (r.Itens ?? []).Where(s => s.PedidoItemId is { } id && validos.Contains(id) && s.Confianca >= ConfiancaParaAplicar))
                if (nota.Itens.FirstOrDefault(i => i.Item == s.Item) is { } it && it.VinculoOrigem != OrigemVinculo.Manual)
                { it.PedidoItemId = s.PedidoItemId; it.VinculoOrigem = OrigemVinculo.Ia; it.Confianca = Math.Round(s.Confianca, 2); }
        }
        await dbw.SaveChangesAsync(ct);
    }

    // ───────────── Mapa de cotação ─────────────

    private sealed record AnaliseMapaIa(string? Resumo, List<string>? PontosAtencao, string? Recomendacao, List<string>? Negociar);

    /// <summary>Análise do mapa entre os fornecedores convidados (preço por item e total, frete, prazo, pagamento, qualificação, histórico).</summary>
    public async Task<string> AnalisarMapaCotacaoAsync(Guid cotacaoId, CancellationToken ct = default)
    {
        string conteudo;
        await using (var db = fabrica.CriarLeitura())
        {
            var k = await db.Cotacoes.AsNoTracking().Include(x => x.Itens).Include(x => x.Fornecedores).ThenInclude(f => f.Fornecedor)
                .FirstOrDefaultAsync(x => x.Id == cotacaoId, ct) ?? throw new RegraDeNegocioException("Cotação não encontrada.");
            var resp = k.Fornecedores.Where(f => k.Itens.Any(i => i.FornecedorId == f.FornecedorId && i.ValorUnitarioFinal > 0)).ToList();
            if (resp.Count == 0) throw new RegraDeNegocioException("Nenhum fornecedor com preço lançado no mapa.");
            var c = new StringBuilder();
            c.AppendLine($"COTAÇÃO {k.Numero} — {k.Nome} · tipo {k.Tipo} · frete {k.Frete} · propostas exigidas {k.PropostasExigidas}.");
            foreach (var f in k.Fornecedores)
            {
                var itens = k.Itens.Where(i => i.FornecedorId == f.FornecedorId).ToList();
                c.AppendLine($"FORNECEDOR {f.Fornecedor.NomeExibicao}: situação do convite {f.Status}, qualificação {f.QualificacaoNaData}, prazo ofertado {f.PrazoEntregaOfertado:dd/MM/yyyy}, " +
                             $"pagamento {f.CondicaoPagamento ?? "não informado"}, frete {(f.ValorFrete ?? 0).ToString("N2", Br)}, validade {f.ValidadeProposta:dd/MM/yyyy}, " +
                             $"total itens {itens.Sum(i => i.ValorTotal).ToString("N2", Br)}.");
                foreach (var i in itens)
                    c.AppendLine($"  item: {i.Descricao} | {i.Quantidade.ToString("0.###", Br)} {i.Unidade} | unit. {i.ValorUnitarioFinal.ToString("N4", Br)} | último preço {i.PrecoUltimo?.ToString("N4", Br) ?? "—"} | referência {i.PrecoReferencia?.ToString("N4", Br) ?? "—"}" +
                                 (i.MaterialControlado ? $" | material controlado, atende especificação: {(i.AtendeEspecificacao is null ? "não avaliado" : i.AtendeEspecificacao.Value ? "sim" : "não")}" : ""));
            }
            conteudo = c.ToString();
        }
        const string instr = "Analise o mapa de cotação para o comprador. Compare total com frete, preço por item (aponte o menor por item e se a divisão entre fornecedores compensa), " +
            "prazo, condição de pagamento, qualificação (fornecedor não qualificado ou vencido não deve ser recomendado em material controlado), variação contra último preço e referência, " +
            "e itens sem preço. Não decida pelo comprador: recomende com ressalvas. Responda só JSON: {\"resumo\": \"...\", \"pontosAtencao\": [\"...\"], " +
            "\"recomendacao\": \"...\", \"negociar\": [\"o que tentar negociar e com quem\"]}";
        var r = await PerguntarJsonAsync<AnaliseMapaIa>("mapa-cotacao", cotacaoId, instr, conteudo, 1500, ct);
        var t = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(r.Resumo)) t.AppendLine(r.Resumo).AppendLine();
        if (r.PontosAtencao is { Count: > 0 }) { t.AppendLine("Pontos de atenção:"); foreach (var p in r.PontosAtencao) t.AppendLine($"• {p}"); t.AppendLine(); }
        if (!string.IsNullOrWhiteSpace(r.Recomendacao)) t.AppendLine($"Recomendação: {r.Recomendacao}").AppendLine();
        if (r.Negociar is { Count: > 0 }) { t.AppendLine("Negociar:"); foreach (var p in r.Negociar) t.AppendLine($"• {p}"); }
        t.AppendLine().Append($"Análise por IA em {relogio.GetLocalNow():dd/MM/yyyy HH:mm} a pedido de {usuario.Nome}. Conferir antes de decidir.");
        var texto = Cortar(t.ToString(), 8000);
        await using var dbw = fabrica.Criar();
        var k2 = await dbw.Cotacoes.FirstAsync(x => x.Id == cotacaoId, ct);
        k2.AnaliseIa = texto; k2.AnaliseIaEm = relogio.GetUtcNow();
        await dbw.SaveChangesAsync(ct);
        return texto;
    }

    // ───────────── RNC ─────────────

    /// <summary>Resumo, causas prováveis (5 porquês), disposição e ações sugeridas para a RNC — a tela preenche o formulário para revisão.</summary>
    public async Task<SugestaoRnc> SugerirRncAsync(string descricao, string? classificacao, string? gravidade, string? local, string? fornecedor, Guid? rncId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Trim().Length < 15) throw new RegraDeNegocioException("Descreva a não conformidade (pelo menos uma frase) antes de pedir a sugestão.");
        var conteudo = $"Não conformidade: {Cortar(descricao, 3000)}\nClassificação: {classificacao}\nGravidade: {gravidade}\nLocal: {local}\nFornecedor/empreiteiro: {fornecedor}";
        const string instr = "Analise a não conformidade de obra. Proponha análise de causa (causas prováveis e 5 porquês), disposição imediata do produto/serviço " +
            "(retrabalho, reparo, aceite com concessão, rejeição), ação corretiva e preventiva práticas, prazo em dias e normas técnicas aplicáveis (NBR) quando houver. " +
            "Responda só JSON: {\"resumo\": \"...\", \"causasProvaveis\": [\"...\"], \"cincoPorques\": [\"...\"], \"disposicao\": \"...\", \"acaoCorretiva\": \"...\", " +
            "\"acaoPreventiva\": \"...\", \"prazoDias\": n, \"referencias\": [\"NBR ...\"]}";
        var r = await PerguntarJsonAsync<SugestaoRnc>("rnc", rncId, instr, conteudo, 1200, ct);
        return r with { CausasProvaveis = r.CausasProvaveis ?? [], CincoPorques = r.CincoPorques ?? [], Referencias = r.Referencias ?? [] };
    }

    // ───────────── Catálogo de produtos ─────────────

    /// <summary>Grupo/subgrupo do catálogo e possíveis duplicados para um produto novo (só sugere; o cadastro decide).</summary>
    public async Task<SugestaoProduto> ClassificarProdutoAsync(string nome, string? descricao, Guid? produtoId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new RegraDeNegocioException("Informe o nome do produto.");
        string conteudo;
        await using (var db = fabrica.CriarLeitura())
        {
            // Grupos e subgrupos em uso no cadastro (os mesmos que a tela oferece).
            var pares = await db.Produtos.AsNoTracking().Where(p => p.Ativo && p.Grupo != null && p.Grupo != "A Cadastrar")
                .Select(p => new { p.Grupo, p.SubGrupo }).Distinct().ToListAsync(ct);
            var arvore = string.Join("\n", pares.GroupBy(x => x.Grupo).OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {string.Join("; ", g.Select(x => x.SubGrupo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Order())}"));
            // Pré-filtro local (sem IA): os 15 produtos mais parecidos pelo nome, para a IA apontar duplicados.
            var todos = await db.Produtos.AsNoTracking().Where(p => p.Ativo && p.Id != produtoId).Select(p => new { p.Codigo, p.Nome, p.Unidade }).ToListAsync(ct);
            var parecidos = todos.Select(p => (p, s: ConferenciaNota.Semelhanca(nome, p.Nome))).Where(x => x.s > 0.2m).OrderByDescending(x => x.s).Take(15).Select(x => $"{x.p.Codigo} | {x.p.Nome} | {x.p.Unidade}");
            conteudo = $"Produto novo: {nome}\nDescrição: {descricao}\n\nGRUPOS E SUBGRUPOS DO CATÁLOGO:\n{arvore}\n\nPRODUTOS PARECIDOS JÁ CADASTRADOS (código | nome | unidade):\n{string.Join("\n", parecidos)}";
        }
        const string instr = "Classifique o produto no catálogo da construtora usando somente grupos e subgrupos da lista (nomes exatos). Sugira a unidade de compra usual. " +
            "Liste como possíveis duplicados apenas produtos da lista que sejam o mesmo item (mesma especificação), pelo código. Responda só JSON: " +
            "{\"grupo\": \"...\"|null, \"subGrupo\": \"...\"|null, \"unidade\": \"...\"|null, \"possiveisDuplicados\": [\"código\"], \"observacao\": \"...\"}";
        var r = await PerguntarJsonAsync<SugestaoProduto>("produto", produtoId, instr, conteudo, 500, ct);
        return r with { PossiveisDuplicados = r.PossiveisDuplicados ?? [] };
    }

    // ───────────── Laudos e certificados (PDF) ─────────────

    private sealed record LaudoIa(string? Tipo, string? Emitente, string? Numero, string? DataEmissao, string? Validade, string? Lote, string? Material,
        List<ResultadoEnsaioLido>? Resultados, bool? Conforme, string? Conclusao);

    /// <summary>Lê o laudo/certificado (PDF ou imagem) e extrai número, datas, lote e resultados dos ensaios para conferência.</summary>
    public async Task<ResultadoLaudo> LerLaudoAsync(Stream arquivo, string contentType, string? especificacao, Guid? referenciaId, CancellationToken ct = default)
    {
        if (!leitor.Configurado) throw new RegraDeNegocioException("Leitura de documentos não configurada (Document Intelligence). Veja o script configurar-ia.sh.");
        await GarantirLimiteAsync(ct);
        (string Texto, int Paginas) lido;
        try { lido = await leitor.ExtrairTextoAsync(arquivo, contentType, ct); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await RegistrarAsync("laudo-leitura", referenciaId, 0, 0, 0, true, ct);
            throw new RegraDeNegocioException("Não foi possível ler o arquivo (PDF protegido ou ilegível?).");
        }
        await RegistrarAsync("laudo-leitura", referenciaId, 0, 0, lido.Paginas, false, ct);
        if (string.IsNullOrWhiteSpace(lido.Texto)) throw new RegraDeNegocioException("O documento não tem texto legível.");
        var conteudo = (string.IsNullOrWhiteSpace(especificacao) ? "" : $"ESPECIFICAÇÃO EXIGIDA:\n{Cortar(especificacao, 2000)}\n\n") + $"DOCUMENTO:\n{Cortar(lido.Texto, 30000)}";
        const string instr = "Extraia do laudo/certificado de material de construção: tipo do documento, laboratório/emitente, número, data de emissão e validade (AAAA-MM-DD), lote, material " +
            "e cada resultado de ensaio (ensaio, valor, unidade, especificação/limite, conforme). Se houver especificação exigida, diga se o documento atende. Não invente valores. " +
            "Responda só JSON: {\"tipo\": \"...\", \"emitente\": \"...\", \"numero\": \"...\", \"dataEmissao\": \"AAAA-MM-DD\"|null, \"validade\": \"AAAA-MM-DD\"|null, \"lote\": \"...\", " +
            "\"material\": \"...\", \"resultados\": [{\"ensaio\": \"...\", \"valor\": \"...\", \"unidade\": \"...\", \"especificacao\": \"...\", \"conforme\": true|false|null}], " +
            "\"conforme\": true|false|null, \"conclusao\": \"...\"}";
        var r = await PerguntarJsonAsync<LaudoIa>("laudo", referenciaId, instr, conteudo, 1500, ct);
        static DateOnly? D(string? s) => DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
        return new ResultadoLaudo(r.Tipo, r.Emitente, r.Numero, D(r.DataEmissao), D(r.Validade), r.Lote, r.Material, r.Resultados ?? [], r.Conforme, r.Conclusao, lido.Paginas);
    }

    // ───────────── DANFE fotografado no recebimento ─────────────

    public sealed record ItemDanfeIa(string? Descricao, string? Unidade, decimal Quantidade, decimal? ValorUnitario);
    public sealed record DanfeIa(string? Chave, string? Numero, string? Serie, string? CnpjEmitente, string? Emitente, decimal? ValorTotal, List<ItemDanfeIa>? Itens);

    /// <summary>Texto (OCR) de uma foto ou PDF pelo Document Intelligence; conta no limite mensal e registra as páginas.</summary>
    public async Task<(string Texto, int Paginas)> LerTextoAsync(Stream arquivo, string contentType, string funcao, Guid? referenciaId, CancellationToken ct = default)
    {
        if (!leitor.Configurado) throw new RegraDeNegocioException("Leitura de documentos não configurada (Document Intelligence). Veja o script configurar-ia.sh.");
        await GarantirLimiteAsync(ct);
        (string Texto, int Paginas) lido;
        try { lido = await leitor.ExtrairTextoAsync(arquivo, contentType, ct); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await RegistrarAsync(funcao, referenciaId, 0, 0, 0, true, ct);
            throw new RegraDeNegocioException("Não foi possível ler a imagem. Tente outra foto, com mais luz e o documento inteiro.");
        }
        await RegistrarAsync(funcao, referenciaId, 0, 0, lido.Paginas, false, ct);
        if (string.IsNullOrWhiteSpace(lido.Texto)) throw new RegraDeNegocioException("A foto não tem texto legível.");
        return lido;
    }

    /// <summary>Organiza o texto do DANFE (chave, número, emitente e itens). A chave volta como lida; quem valida o DV é quem chama.</summary>
    public async Task<DanfeIa> ExtrairDanfeAsync(string texto, Guid? referenciaId, CancellationToken ct = default)
    {
        const string instr = "O texto abaixo é o OCR de uma foto de DANFE (nota fiscal eletrônica). Extraia a chave de acesso (44 dígitos, só números), número, série, " +
            "CNPJ do emitente (só números), nome do emitente, valor total e os itens (descrição, unidade, quantidade, valor unitário) como estão na nota. " +
            "Use ponto como separador decimal. Não invente: campo ilegível fica null. Responda só JSON: {\"chave\": \"...\"|null, \"numero\": \"...\", \"serie\": \"...\", " +
            "\"cnpjEmitente\": \"...\", \"emitente\": \"...\", \"valorTotal\": n|null, \"itens\": [{\"descricao\": \"...\", \"unidade\": \"...\", \"quantidade\": n, \"valorUnitario\": n|null}]}";
        return await PerguntarJsonAsync<DanfeIa>("danfe", referenciaId, instr, Cortar(texto, 20000), 2000, ct);
    }

    private static string Cortar(string s, int max) => s.Length <= max ? s : s[..max];
}

/// <summary>Limite de chamadas de IA por construtora no mês (controle de custo; configuração IA:LimiteMensalPorEmpresa).</summary>
public sealed record LimitesIa(int ChamadasPorMes);
