using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Sst;
using Sigeo.Domain.Suprimentos;
using Sigeo.Infrastructure.Servicos;
using Sigeo.Web.Components.Base;
using Sigeo.Web.Components.Layout;
using Sigeo.Web.Components.Shared;
using Sigeo.Web.Components;
using Sigeo.Web.Seguranca;
using Sigeo.Web.Servicos;
using Sigeo.Web;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using static Sigeo.Web.Servicos.Rotulos;

namespace Sigeo.Web.Components.Pages.Suprimentos;

// Código da tela Recebimentos.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class Recebimentos
{
    private sealed record Linha(Guid Id, int Numero, DateTimeOffset Data, int? Pedido, string Fornecedor, string? Nf, bool? Aprovada, decimal? Nota, DestinoRecebimento Destino, string CriadoPor);
    private Grade<Linha>? _grade;
    private List<Linha>? _itens;
    private Recebimento? _r;
    private bool _aberto;
    private List<Opcao> _pedidos = [];
    private bool Novo_ => _r is { Numero: 0 };

    private IEnumerable<(string, string, Func<ResultadoCriterio>, Action<ResultadoCriterio>, Func<decimal?>, Action<decimal?>)> Criterios() =>
    [
        ("Prazo", "Entregue na data combinada", () => _r!.FvmPrazo, v => _r!.FvmPrazo = v, () => _r!.NotaPrazo, v => _r!.NotaPrazo = v),
        ("Qualidade", "Conforme especificação, sem avarias (1ª inspeção)", () => _r!.FvmQualidade, v => _r!.FvmQualidade = v, () => _r!.NotaQualidade, v => _r!.NotaQualidade = v),
        ("Documentação / atendimento", "Documentos completos no ato, quantidade, descarga", () => _r!.FvmAtendimento, v => _r!.FvmAtendimento = v, () => _r!.NotaDocumentacao, v => _r!.NotaDocumentacao = v),
    ];

    private ParametrosVigentes? _par;

    /// <summary>Nota digitada (0–10) define o critério na hora, como o servidor fará ao salvar.</summary>
    private void Nota(Func<decimal?> get, Action<decimal?> set, Action<ResultadoCriterio> res, string? texto)
    {
        decimal? v = decimal.TryParse((texto ?? "").Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? Math.Clamp(d, 0, 10) : null;
        set(v);
        if (v is { } n) res(n >= (_par?[Parametros.IdfMinimo] ?? 6m) ? ResultadoCriterio.Aprovado : ResultadoCriterio.Reprovado);
        var p = _par ?? ParametrosVigentes.Padrao;
        _r!.AplicarNotas(p[Parametros.PesoIdfPrazo], p[Parametros.PesoIdfQualidade], p[Parametros.PesoIdfDocumentacao], p[Parametros.IdfMinimo]);
    }

    private List<Almoxarifado> _almoxs = [];
    private List<string> _marcas = [];
    private readonly Dictionary<Guid, string?> _marcaPedido = [];
    private bool DivergeMarca(RecebimentoItem i) => i.PedidoItemId is { } p && _marcaPedido.TryGetValue(p, out var m) && !string.IsNullOrWhiteSpace(m)
        && !string.IsNullOrWhiteSpace(i.Marca) && Marca.Chave(m) != Marca.Chave(i.Marca);

    protected override async Task OnInitializedAsync()
    {
        _almoxs = await EstoqueSrv.AlmoxarifadosAsync(Cancelamento);
        await using (var dbp = Fabrica.CriarLeitura()) _par = await ParametrosServico.CarregarAsync(dbp, Cancelamento);
        _marcas = await Cat.MarcasAsync();
        await Carregar();
        if (Nav.Uri.Contains("?pedido=") && Guid.TryParse(Nav.Uri.Split("?pedido=")[1], out var pid)) { await Novo(); await EscolherPedido(pid.ToString()); }
        else if (Nav.Uri.Contains("?nota=") && Guid.TryParse(Nav.Uri.Split("?nota=")[1], out var nid)) await NovoDaNota(nid);
    }

    /// <summary>
    /// Recebimento a partir da NF-e importada (SIEG/XML) já ligada ao pedido: chave e número da nota preenchidos e a quantidade
    /// de cada item vinda da nota (itens do pedido sem correspondente na nota ficam com zero). O almoxarife confere e avalia a FVM.
    /// </summary>
    private async Task NovoDaNota(Guid notaId)
    {
        await using var db = Fabrica.CriarLeitura();
        var n = await db.NotasFiscais.AsNoTracking().Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == notaId, Cancelamento);
        if (n?.PedidoId is not { } pedidoId || n.Situacao != Sigeo.Domain.Fiscal.SituacaoNota.Vinculada) { Notif.Erro("Nota sem pedido vinculado (ou já recebida)."); return; }
        await Novo();
        await EscolherPedido(pedidoId.ToString());
        if (_r is null) return;
        _r.ChaveNFe = n.Chave; _r.NumeroNotaFiscal = n.Numero.ToString();
        var porItem = n.Itens.Where(i => i.PedidoItemId != null).GroupBy(i => i.PedidoItemId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var avisos = new List<string>();
        foreach (var ri in _r.Itens)
        {
            if (ri.PedidoItemId is { } pi && porItem.TryGetValue(pi, out var nfs))
            {
                ri.QtdRecebida = nfs.Sum(x => x.Quantidade);
                if (nfs.Any(x => !Sigeo.Domain.Fiscal.ConferenciaNota.MesmaUnidade(x.Unidade, ri.Unidade)))
                    avisos.Add($"{ri.Descricao}: unidade da nota ({string.Join("/", nfs.Select(x => x.Unidade).Distinct())}) diferente do pedido ({ri.Unidade}) — confira a quantidade.");
                if (ri.QtdRecebida > ri.QtdPedida) avisos.Add($"{ri.Descricao}: nota com {ri.QtdRecebida:0.###} e saldo do pedido {ri.QtdPedida:0.###}.");
            }
            else ri.QtdRecebida = 0;
        }
        var semPedido = n.Itens.Count(i => i.PedidoItemId is null);
        if (semPedido > 0) avisos.Add($"{semPedido} item(ns) da nota sem item do pedido correspondente.");
        if (avisos.Count > 0) ErrosForm = avisos;
        Notif.Aviso($"Recebimento preenchido pela NF {n.Numero}: confira as quantidades e avalie a FVM.");
    }

    private async Task Carregar()
    {
        await using var db = Fabrica.CriarLeitura();
        var lista = await db.Recebimentos.AsNoTracking().Include(r => r.Pedido).Include(r => r.Itens).OrderByDescending(r => r.Numero).Take(LimiteAtual).ToListAsync(Cancelamento);
        var forn = await db.Fornecedores.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NomeExibicao, Cancelamento);
        _itens = lista.Select(r => new Linha(r.Id, r.Numero, r.DataRecebimento, r.Pedido?.Numero, forn.GetValueOrDefault(r.FornecedorId, ""), r.NumeroNotaFiscal ?? r.ChaveNFe,
            r.FvmAprovada, r.NotaEntrega, r.Destino, r.RecebidoPorNome)).ToList();
    }

    private async Task Novo()
    {
        await using var db = Fabrica.CriarLeitura();
        _pedidos = await db.Pedidos.AsNoTracking().Where(p => p.Status == StatusPedido.EnviadoFornecedor || p.Status == StatusPedido.EntregueParcial)
            .OrderBy(p => p.Numero).Select(p => new Opcao(p.Id, "Pedido " + p.Numero + " – " + p.Fornecedor.RazaoSocial)).ToListAsync(Cancelamento);
        _r = new Recebimento { DataRecebimento = DateTimeOffset.Now };
        _fotoNota = null; _itensLidos = null;
        ErrosForm = null; _aberto = true;
    }

    private async Task EscolherPedido(string? valor)
    {
        _r!.Itens.Clear(); _r.PedidoId = null;
        if (!Guid.TryParse(valor, out var id)) return;
        await using var db = Fabrica.CriarLeitura();
        var p = await db.Pedidos.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == id, Cancelamento);
        if (_pedidos.All(o => o.Id != id)) _pedidos.Add(new Opcao(p.Id, $"Pedido {p.Numero}"));
        _r.PedidoId = p.Id; _r.FornecedorId = p.FornecedorId; _r.ObraId = p.ObraId;
        _r.AlmoxarifadoId = p.EntregaAlmoxarifadoCentral ? _almoxs.FirstOrDefault(a => a.Tipo == TipoAlmoxarifado.Central && a.Ativo)?.Id : _almoxs.FirstOrDefault(a => a.ObraId == p.ObraId)?.Id;
        foreach (var i in p.Itens.Where(i => i.Quantidade > i.QuantidadeRecebidaAceita))
        {
            var saldo = i.Quantidade - i.QuantidadeRecebidaAceita;
            _marcaPedido[i.Id] = i.Marca;
            _r.Itens.Add(new RecebimentoItem { PedidoItemId = i.Id, Descricao = i.Descricao, Unidade = i.Unidade, QtdPedida = saldo, QtdRecebida = saldo, Marca = i.Marca,
                MaterialControlado = i.MaterialControladoId is not null, MaterialControladoId = i.MaterialControladoId });
        }
        _r.FvmItens.Clear(); _modelo = null;
        if (_r.Itens.FirstOrDefault(i => i.MaterialControladoId is not null)?.MaterialControladoId is { } mat && await Qob.ModeloFvmVigenteAsync(mat, Cancelamento) is { } m)
        {
            _modelo = m;
            foreach (var it in m.Itens.OrderBy(x => x.Ordem))
                _r.FvmItens.Add(new FvmItem { RecebimentoId = _r.Id, ItemVerificacaoId = it.Id, Ordem = it.Ordem, Descricao = it.Descricao, Criterio = it.Criterio, ValorMin = it.ValorMin, ValorMax = it.ValorMax, Bloqueia = it.Bloqueia });
        }
        if (_itensLidos is { Count: > 0 }) AplicarItensLidos(_itensLidos);
    }

    private ModeloInspecao? _modelo;
    private readonly Dictionary<Guid, string> _resEnsaio = [];
    private Dictionary<Guid, string> _locNomes = [];
    private List<Opcao> _locais = [];
    private Guid? _apLocal;
    private decimal _apQtd;
    private DateOnly _apData;

    /// <summary>Laudo lido pela IA: sugere o resultado de cada ensaio aguardando (pelo nome mais parecido). Aprovar/reprovar continua com a pessoa.</summary>
    private void PreencherEnsaios(Sigeo.Application.IA.ResultadoLaudo laudo)
    {
        var n = 0;
        foreach (var en in _r!.Ensaios.Where(x => x.Situacao == SituacaoEnsaio.Aguardando))
        {
            var melhor = laudo.Resultados.Select(x => (x, s: Sigeo.Domain.Fiscal.ConferenciaNota.Semelhanca(en.Ensaio, x.Ensaio))).OrderByDescending(x => x.s).FirstOrDefault();
            if (melhor.x is { } res && melhor.s >= 0.25m && !string.IsNullOrWhiteSpace(res.Valor))
            {
                _resEnsaio[en.Id] = $"{res.Valor} {res.Unidade}".Trim() + (laudo.Numero is null ? "" : $" (laudo {laudo.Numero})");
                n++;
            }
        }
        if (n > 0) Notif.Aviso($"{n} resultado(s) preenchido(s) pelo laudo — confira e aprove ou reprove cada ensaio.");
        else Notif.Aviso("O laudo não trouxe resultado para os ensaios previstos: confira manualmente.");
    }

    private async Task Ensaio(Guid id, bool aprovado)
    {
        if (await Executar(() => Qob.RegistrarEnsaioAsync(id, _resEnsaio.GetValueOrDefault(id) ?? "", aprovado, null, Cancelamento), aprovado ? "Ensaio aprovado." : "Ensaio reprovado — RNC aberta com os locais afetados."))
            await Abrir(_r!.Id);
    }

    private async Task Aplicar()
    {
        if (_apLocal is null) { Notif.Erro("Informe o local."); return; }
        if (await Executar(() => Qob.AplicarLoteAsync(_r!.Id, _apLocal.Value, _apQtd, _apData, null, Cancelamento), "Aplicação registrada.")) await Abrir(_r!.Id);
    }

    private async Task Abrir(Guid id)
    {
        await using var db = Fabrica.CriarLeitura();
        _r = await db.Recebimentos.AsNoTracking().Include(r => r.Itens).Include(r => r.FvmItens).Include(r => r.Ensaios).Include(r => r.Aplicacoes).AsSplitQuery().FirstAsync(r => r.Id == id, Cancelamento);
        _locais = await Cat.LocaisAsync(_r.ObraId);
        _locNomes = _locais.ToDictionary(l => l.Id, l => l.Texto);
        var pis = _r.Itens.Where(i => i.PedidoItemId != null).Select(i => i.PedidoItemId!.Value).ToList();
        _marcaPedido.Clear();
        foreach (var pm in await db.PedidoItens.AsNoTracking().Where(i => pis.Contains(i.Id)).Select(i => new { i.Id, i.Marca }).ToListAsync(Cancelamento)) _marcaPedido[pm.Id] = pm.Marca;
        _resEnsaio.Clear(); foreach (var en in _r.Ensaios) _resEnsaio[en.Id] = en.Resultado ?? "";
        _apLocal = null; _apQtd = 0; _apData = Hoje;
        var ped = _r.PedidoId is { } pid ? await db.Pedidos.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Numero).FirstOrDefaultAsync(Cancelamento) : 0;
        _pedidos = _r.PedidoId is { } x ? [new Opcao(x, $"Pedido {ped}")] : [];
        _transf = await db.Transferencias.AsNoTracking().Where(t => t.RecebimentoId == id && t.Status != StatusTransferencia.Cancelada).Select(t => (Guid?)t.Id).FirstOrDefaultAsync(Cancelamento);
        ErrosForm = null; _aberto = true;
    }

    private Guid? _transf;

    private async Task Transferir()
    {
        if (_r is not { AlmoxarifadoCentral: true }) return;
        Guid id = default;
        if (await Executar(async () => id = await TransferenciasSrv.GerarTransferenciaAsync(_r!.Id, Cancelamento), "Transferência montada — confira e envie.")) Nav.NavigateTo($"transferencias?id={id}");
    }

    private async Task Registrar()
    {
        Guid id = default;
        var foto = _fotoNota;
        if (await Executar(async () => id = await RecebimentosSrv.RegistrarRecebimentoAsync(_r!, Cancelamento), "Recebimento registrado."))
        {
            if (foto is not null)
                try
                {
                    await AnexosSrv.AnexarAsync("Recebimento", id, "Documentos", "recebimento", AnexosServico.LimiteFotoBytes,
                        [new ArquivoEnviado($"DANFE NF {_r!.NumeroNotaFiscal} (foto).jpg", foto.Length, "image/jpeg", _ => new MemoryStream(foto))], Cancelamento);
                }
                catch (Exception) { Notif.Aviso("Recebimento registrado, mas a foto da nota não foi anexada: anexe pelo quadro de documentos."); }
            if (_r!.FvmAprovada == false) Notif.Aviso("FVM reprovada: RNC e plano de ação abertos para o fornecedor.");
            await Carregar(); await Abrir(id);
        }
    }

    // ───────────── Leitura da nota (câmera, chave digitada ou leitor de código de barras) ─────────────

    private byte[]? _fotoNota;
    private IReadOnlyList<Sigeo.Application.Fiscal.ItemNotaLido>? _itensLidos;

    private async Task ChaveDigitada(string? valor)
    {
        _r!.ChaveNFe = string.IsNullOrWhiteSpace(valor) ? null : Sigeo.Domain.Fiscal.ChaveNfe.SoDigitos(valor);
        if (_r.ChaveNFe is not { Length: 44 }) return;
        if (!Sigeo.Domain.Fiscal.ChaveNfe.Valida(_r.ChaveNFe)) { Notif.Erro("Chave da NF-e com dígito verificador errado: confira a digitação."); return; }
        try { await AplicarLeitura(new LeituraNotaCamera.NotaFotografada(await LeituraNota.ResolverChaveAsync(_r.ChaveNFe, Cancelamento), null)); }
        catch (Sigeo.Domain.Aprovacoes.RegraDeNegocioException ex) { Notif.Erro(ex.Message); }
    }

    /// <summary>
    /// Nota lida: importada e ligada ao pedido → recebimento inteiro pela nota (como em Notas fiscais › Receber).
    /// Senão: chave e número preenchidos; pedido escolhido se o fornecedor tiver um só aguardando entrega (com mais de um, a lista
    /// fica só com os dele); itens lidos pela IA entram como quantidade recebida pelo nome mais parecido.
    /// </summary>
    private async Task AplicarLeitura(LeituraNotaCamera.NotaFotografada x)
    {
        var n = x.Nota;
        if (n.RecebimentoExistente is { } rn) { Notif.Erro($"NF {n.Numero} já recebida no recebimento {rn}."); return; }
        if (n.Situacao == Sigeo.Domain.Fiscal.SituacaoNota.Cancelada) { Notif.Erro($"NF {n.Numero} cancelada na SEFAZ: não receba com ela."); return; }
        if (n.ProntaParaReceber)
        {
            await NovoDaNota(n.NotaId!.Value);
            _fotoNota = x.Foto ?? _fotoNota;
            return;
        }
        _fotoNota = x.Foto ?? _fotoNota;
        _itensLidos = n.Itens.Count > 0 ? n.Itens : null;
        var avisos = n.Avisos.ToList();
        if (n.PedidosAbertos.Count == 1)
        {
            if (_r!.PedidoId != n.PedidosAbertos[0].Id) await EscolherPedido(n.PedidosAbertos[0].Id.ToString());
            else if (_itensLidos is not null) AplicarItensLidos(_itensLidos);
        }
        else if (n.PedidosAbertos.Count > 1)
        {
            if (_r!.PedidoId is not { } atual || n.PedidosAbertos.All(p => p.Id != atual))
            {
                _pedidos = n.PedidosAbertos.Select(p => new Opcao(p.Id, $"Pedido {p.Numero} – {p.Obra}")).ToList();
                _r.Itens.Clear(); _r.PedidoId = null;
                avisos.Insert(0, $"{n.Fornecedor} tem {n.PedidosAbertos.Count} pedidos aguardando entrega: escolha o pedido desta nota.");
            }
            else if (_itensLidos is not null) AplicarItensLidos(_itensLidos);
        }
        _r!.ChaveNFe = n.Chave; _r.NumeroNotaFiscal = n.Numero;
        ErrosForm = (ErrosForm ?? []).Concat(avisos).Distinct().ToList() is { Count: > 0 } l ? l : null;
        Notif.Aviso($"NF {n.Numero} lida por {n.Origem}: confira o pedido e as quantidades.");
    }

    /// <summary>Quantidade de cada item do pedido pelo item lido de nome mais parecido (um para um); sem correspondente fica zero.</summary>
    private void AplicarItensLidos(IReadOnlyList<Sigeo.Application.Fiscal.ItemNotaLido> lidos)
    {
        if (_r is null || _r.Itens.Count == 0) return;
        var pares = (from ri in _r.Itens from l in lidos
                     let s = Sigeo.Domain.Fiscal.ConferenciaNota.Semelhanca(ri.Descricao, l.Descricao) + (Sigeo.Domain.Fiscal.ConferenciaNota.MesmaUnidade(l.Unidade, ri.Unidade) ? 0.1m : 0)
                     where s >= 0.35m orderby s descending select (ri, l)).ToList();
        var usadosR = new HashSet<RecebimentoItem>(); var usadosL = new HashSet<Sigeo.Application.Fiscal.ItemNotaLido>();
        var avisos = new List<string>();
        foreach (var (ri, l) in pares)
        {
            if (usadosR.Contains(ri) || usadosL.Contains(l)) continue;
            usadosR.Add(ri); usadosL.Add(l);
            ri.QtdRecebida = l.Quantidade;
            if (!Sigeo.Domain.Fiscal.ConferenciaNota.MesmaUnidade(l.Unidade, ri.Unidade)) avisos.Add($"{ri.Descricao}: unidade da nota ({l.Unidade}) diferente do pedido ({ri.Unidade}) — confira a quantidade.");
            if (l.Quantidade > ri.QtdPedida) avisos.Add($"{ri.Descricao}: nota com {l.Quantidade:0.###} e saldo do pedido {ri.QtdPedida:0.###}.");
        }
        foreach (var ri in _r.Itens.Where(i => !usadosR.Contains(i))) ri.QtdRecebida = 0;
        var sobra = lidos.Count(l => !usadosL.Contains(l));
        if (sobra > 0) avisos.Add($"{sobra} item(ns) lido(s) na nota sem item do pedido correspondente.");
        avisos.Add("Quantidades preenchidas pela leitura da foto (IA): confira cada item com a nota.");
        ErrosForm = (ErrosForm ?? []).Concat(avisos).Distinct().ToList();
    }
}
