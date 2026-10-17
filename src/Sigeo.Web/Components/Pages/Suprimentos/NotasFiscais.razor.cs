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
using Sigeo.Application.Fiscal;
using Sigeo.Contratos;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Fiscal;
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

// Código da tela NotasFiscais.razor (a marcação fica no .razor; o VS Code agrupa os dois).
public partial class NotasFiscais
{
    private static readonly System.Globalization.CultureInfo Br = new("pt-BR");
    [SupplyParameterFromQuery(Name = "id")] public Guid? IdQuery { get; set; }
    [Inject] private NotasFiscaisConsultas Consultas { get; set; } = default!;

    private string? _fSit = "fila", _busca, _motivo;
    private List<NotaFiscalEntrada> _lista = [];
    private List<ImportacaoNota> _importacoes = [];
    private Dictionary<Guid, int> _numPedido = [];
    private NotaFiscalEntrada? _n;
    private List<Opcao> _pedidosForn = [];
    private List<ItemPedidoParaConferencia> _itensPedido = [];
    private Guid? _pedidoEscolhido;
    private bool _aberto, _abertoIgnorar, _siegOk, _iaOk;
    private DateOnly _de, _ate;
    private ResultadoImportacaoNotas? _resultado;
    private int _kSemPedido, _kVinculadas, _kCanceladas;
    private decimal _kValorFila;

    private static string Cnpj(string? c) => c is { Length: 14 } ? $"{c[..2]}.{c[2..5]}.{c[5..8]}/{c[8..12]}-{c[12..]}" : c ?? "";

    protected override async Task OnInitializedAsync()
    {
        _siegOk = Fonte.Configurada(Usuario.EmpresaId);
        _iaOk = Servicos.GetService<Sigeo.Application.Abstracoes.IAssistenteIa>()?.Configurado == true;
        _ate = Hoje; _de = Hoje.AddDays(-7);
        await Carregar();
        if (IdQuery is { } id) await Abrir(id);
    }

    private async Task Carregar()
    {
        (_lista, _numPedido, _kSemPedido, _kVinculadas, _kValorFila, _kCanceladas, _importacoes) = await Consultas.ListaAsync(_fSit, LimiteAtual, Cancelamento);
    }

    private IEnumerable<NotaFiscalEntrada> Filtradas()
    {
        if (string.IsNullOrWhiteSpace(_busca)) return _lista;
        var b = Rotulos.SemAcento(_busca.Trim());
        return _lista.Where(n => Rotulos.SemAcento($"{n.Numero} {n.EmitenteNome} {n.EmitenteCnpj} {n.Chave}").Contains(b));
    }

    private async Task Importar(InputFileChangeEventArgs e)
    {
        _resultado = null;
        await Executar(async () =>
        {
            const long max = 20 * 1024 * 1024;
            if (e.File.Size > max) throw new RegraDeNegocioException("Arquivo acima de 20 MB: divida o ZIP.");
            await using var s = e.File.OpenReadStream(max, Cancelamento);
            _resultado = await Notas.ImportarArquivoAsync(s, e.File.Name, Cancelamento);
        });
        await Carregar();
    }

    private async Task BuscarSieg()
    {
        _resultado = null;
        Notif.Aviso("Buscando no SIEG… (o SIEG aceita poucas consultas por minuto: pode levar alguns minutos)");
        await Executar(async () => _resultado = await Notas.BuscarNaFonteAsync(Fonte, Usuario.EmpresaId, _de, _ate, Cancelamento));
        await Carregar();
    }

    private async Task Abrir(Guid id)
    {
        var c = await Consultas.NotaAsync(id, Cancelamento);
        _n = c?.Nota;
        if (c is null) { Notif.Erro("Nota não encontrada."); return; }
        _pedidoEscolhido = c.Nota.PedidoId;
        _pedidosForn = c.PedidosFornecedor;
        _itensPedido = c.ItensPedido;
        ErrosForm = null; _aberto = true;
    }

    private async Task Vincular()
    {
        if (await Executar(() => Notas.VincularPedidoAsync(_n!.Id, _pedidoEscolhido!.Value, ct: Cancelamento), "Nota vinculada ao pedido — confira os itens.")) { await Abrir(_n!.Id); await Carregar(); }
    }

    private void Desvincular() => Confirmar("Desvincular a nota do pedido?", async () =>
    {
        if (await Executar(() => Notas.DesvincularAsync(_n!.Id, Cancelamento), "Nota desvinculada.")) { await Abrir(_n!.Id); await Carregar(); }
    });

    private async Task DefinirItem(int item, string? valor)
    {
        Guid? pi = Guid.TryParse(valor, out var g) ? g : null;
        if (await Executar(() => Notas.DefinirItemAsync(_n!.Id, item, pi, Cancelamento))) await Abrir(_n!.Id);
    }

    private void AbrirIgnorar() { _motivo = null; ErrosForm = null; _abertoIgnorar = true; }

    private async Task Ignorar()
    {
        if (await Executar(() => Notas.IgnorarAsync(_n!.Id, _motivo ?? "", Cancelamento), "Nota fora da fila.")) { _abertoIgnorar = false; _aberto = false; await Carregar(); }
    }

    private async Task VoltarFila()
    {
        if (await Executar(() => Notas.VoltarParaFilaAsync(_n!.Id, Cancelamento), "Nota de volta à fila.")) { await Abrir(_n!.Id); await Carregar(); }
    }

    private void GerarRecebimento() => Nav.NavigateTo($"recebimentos?nota={_n!.Id}");

    private async Task AnalisarIa()
    {
        var ia = Servicos.GetRequiredService<Sigeo.Application.IA.AnalisesIaServico>();
        Notif.Aviso("Analisando com IA…");
        if (await Executar(() => ia.AnalisarNotaAsync(_n!.Id, Cancelamento), "Análise pronta — confira as sugestões.")) await Abrir(_n!.Id);
    }
}
