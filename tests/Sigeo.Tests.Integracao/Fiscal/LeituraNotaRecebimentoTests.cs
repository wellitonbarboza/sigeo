using System.Security.Cryptography;
using Sigeo.Application.Fiscal;
using Sigeo.Application.IA;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Fiscal;
using Sigeo.Domain.Suprimentos;
using Sigeo.Infrastructure.Servicos;

namespace Sigeo.Tests.Integracao;

/// <summary>Leitura da NF-e no recebimento (chave pela câmera/leitor/digitação e foto pela IA) contra o banco real. Dados fictícios.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class LeituraNotaRecebimentoTests(BancoFixture f)
{
    private static string ChaveDe(string cnpjEmitente, int numero)
    {
        var c43 = "41" + "2610" + cnpjEmitente + "55" + "001" + numero.ToString("D9") + "1" + RandomNumberGenerator.GetInt32(10_000_000, 99_999_999);
        return c43 + ChaveNfe.DigitoVerificador(c43);
    }

    private sealed class OcrFalso(string texto) : ILeitorDocumentos
    {
        public bool Configurado => true;
        public Task<(string Texto, int Paginas)> ExtrairTextoAsync(Stream arquivo, string contentType, CancellationToken ct = default) => Task.FromResult((texto, 1));
    }

    private sealed class ModeloFalso(string json) : IAssistenteIa
    {
        public int Chamadas;
        public bool Configurado => true;
        public Task<RespostaIa> PerguntarAsync(string instrucoes, string conteudo, bool json1, int max, CancellationToken ct = default)
        { Chamadas++; return Task.FromResult(new RespostaIa(json, 100, 50)); }
    }

    private LeituraNotaServico Servico(ILeitorDocumentos ocr, IAssistenteIa modelo) =>
        new(f.Fabrica, new LeitorImagemNota(), new AnalisesIaServico(f.Fabrica, modelo, ocr, f.Usuario, f.Relogio, new LimitesIa(100_000)), ocr, modelo);

    private async Task<(Guid Forn, string Cnpj, Pedido P1, string CnpjDest)> MontarAsync(int pedidos)
    {
        var obra = await Dados.CriarObraAsync(f);
        var fornId = await Dados.CriarFornecedorAsync(f);
        var cnpjDest = Dados.Cnpj();
        Pedido? primeiro = null;
        await using var db = f.Fabrica.Criar();
        var cnpj = Application.Comum.Conversor.SomenteDigitos((await db.Fornecedores.SingleAsync(x => x.Id == fornId)).CpfCnpj);
        db.Estabelecimentos.Add(new Estabelecimento { Codigo = $"L{Dados.Sufixo()}", RazaoSocial = "Filial Ficticia", Cnpj = cnpjDest, Raiz = cnpjDest[..8] });
        for (var i = 0; i < pedidos; i++)
        {
            var p = new Pedido { ObraId = obra, Numero = RandomNumberGenerator.GetInt32(500_000, 900_000), FornecedorId = fornId, Entrega = DateOnly.FromDateTime(DateTime.Today), Status = StatusPedido.EnviadoFornecedor };
            p.Itens.Add(new PedidoItem { PedidoId = p.Id, Descricao = "Cimento CP II-F-32 saco 50 kg", Unidade = "SC", Quantidade = 100, ValorUnitario = 38 });
            p.Itens.Add(new PedidoItem { PedidoId = p.Id, Descricao = "Areia média lavada", Unidade = "M3", Quantidade = 12, ValorUnitario = 120 });
            db.Pedidos.Add(p);
            primeiro ??= p;
        }
        await db.SaveChangesAsync();
        return (fornId, cnpj, primeiro!, cnpjDest);
    }

    [FatoComBanco]
    public async Task Chave_de_nota_importada_e_vinculada_fica_pronta_para_receber_e_duplicidade_e_avisada()
    {
        var (forn, cnpj, pedido, dest) = await MontarAsync(1);
        var numero = RandomNumberGenerator.GetInt32(1, 99999);
        var chave = ChaveDe(cnpj, numero);
        var xml = $"""
            <nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00"><NFe><infNFe Id="NFe{chave}" versao="4.00">
              <ide><mod>55</mod><serie>1</serie><nNF>{numero}</nNF><dhEmi>2026-10-05T09:30:00-03:00</dhEmi></ide>
              <emit><CNPJ>{cnpj}</CNPJ><xNome>Fornecedor Ficticio</xNome></emit><dest><CNPJ>{dest}</CNPJ></dest>
              <det nItem="1"><prod><cProd>C1</cProd><xProd>CIMENTO CP II SACO 50KG</xProd><uCom>SC</uCom><qCom>40</qCom><vUnCom>38</vUnCom><vProd>1520.00</vProd></prod></det>
              <total><ICMSTot><vNF>1520.00</vNF></ICMSTot></total><infAdic><infCpl>Pedido nº {pedido.Numero}</infCpl></infAdic>
            </infNFe></NFe></nfeProc>
            """;
        var pasta = Path.Combine(Path.GetTempPath(), "qh-leitura-" + Guid.NewGuid().ToString("N"));
        await new NotasFiscaisServico(f.Fabrica, new ArmazenamentoLocal(pasta), f.Relogio).ImportarAsync([LeitorNfe.LerTexto(xml, "n.xml")], OrigemNota.Manual, null, null, null);

        var svc = Servico(new OcrFalso(""), new ModeloFalso("{}"));
        var r = await svc.ResolverChaveAsync(string.Join(" ", Enumerable.Range(0, 11).Select(i => chave.Substring(i * 4, 4))));
        Assert.True(r.ProntaParaReceber);
        Assert.Equal(pedido.Id, r.PedidoDaNotaId);
        Assert.Equal(forn, r.FornecedorId);
        Assert.Equal(numero.ToString(), r.Numero);
        Assert.Null(r.RecebimentoExistente);

        await using (var db = f.Fabrica.Criar())
        {
            db.Recebimentos.Add(new Recebimento { ObraId = pedido.ObraId, PedidoId = pedido.Id, FornecedorId = forn, ChaveNFe = chave, Numero = RandomNumberGenerator.GetInt32(800_000, 900_000), DataRecebimento = DateTimeOffset.Now });
            await db.SaveChangesAsync();
        }
        var r2 = await svc.ResolverChaveAsync(chave);
        Assert.NotNull(r2.RecebimentoExistente);
        Assert.Contains(r2.Avisos, a => a.Contains("já foi recebida"));

        await Assert.ThrowsAsync<Domain.Aprovacoes.RegraDeNegocioException>(() => svc.ResolverChaveAsync(chave[..43] + (chave[43] == '0' ? '1' : '0')));
        Directory.Delete(pasta, true);
    }

    [FatoComBanco]
    public async Task Nota_nao_importada_traz_fornecedor_e_pedidos_e_fornecedor_desconhecido_e_avisado()
    {
        var (forn, cnpj, _, _) = await MontarAsync(2);
        var svc = Servico(new OcrFalso(""), new ModeloFalso("{}"));
        var r = await svc.ResolverChaveAsync(ChaveDe(cnpj, 321));
        Assert.Null(r.NotaId);
        Assert.False(r.ProntaParaReceber);
        Assert.Equal(forn, r.FornecedorId);
        Assert.Equal(2, r.PedidosAbertos.Count);
        Assert.Contains(r.Avisos, a => a.Contains("não importada"));

        var outro = await svc.ResolverChaveAsync(ChaveDe(Dados.Cnpj(), 322));
        Assert.Null(outro.FornecedorId);
        Assert.Empty(outro.PedidosAbertos);
        Assert.Contains(outro.Avisos, a => a.Contains("não está cadastrado"));
    }

    [FatoComBanco]
    public async Task Foto_lida_pela_ia_traz_chave_e_itens_e_registra_o_uso()
    {
        var (forn, cnpj, _, _) = await MontarAsync(1);
        var chave = ChaveDe(cnpj, 777);
        var modelo = new ModeloFalso("{\"chave\": \"" + chave + "\", \"numero\": \"777\", \"serie\": \"1\", \"cnpjEmitente\": \"" + cnpj + "\", \"emitente\": \"Fornecedor Ficticio\", " +
            "\"itens\": [{\"descricao\": \"CIMENTO CP II 50KG\", \"unidade\": \"SC\", \"quantidade\": 40, \"valorUnitario\": 38}, {\"descricao\": \"AREIA MEDIA\", \"unidade\": \"M3\", \"quantidade\": 6}]}");
        // OCR sem a chave legível: o modelo é chamado e a chave dele passa pelo DV.
        var svc = Servico(new OcrFalso("DANFE documento auxiliar NF-e 777 Fornecedor Ficticio"), modelo);
        var foto = new LeitorImagemNota().Reduzir(FotoBranca());
        var antes = await ContarUsosAsync();
        var r = await svc.LerComIaAsync(foto, null);
        Assert.Equal(chave, r.Chave);
        Assert.Equal(forn, r.FornecedorId);
        Assert.Equal(2, r.Itens.Count);
        Assert.Equal(40, r.Itens[0].Quantidade);
        Assert.Equal(1, modelo.Chamadas);
        Assert.Equal(antes + 2, await ContarUsosAsync()); // leitura do texto + modelo

        // Chave lida pela IA com DV errado não é aceita.
        var ruim = new ModeloFalso("{\"chave\": \"" + chave[..43] + (chave[43] == '0' ? '1' : '0') + "\", \"itens\": []}");
        await Assert.ThrowsAsync<Domain.Aprovacoes.RegraDeNegocioException>(() => Servico(new OcrFalso("DANFE"), ruim).LerComIaAsync(foto, null));
    }

    private async Task<int> ContarUsosAsync()
    {
        await using var db = f.Fabrica.CriarLeitura();
        return await db.UsosIa.CountAsync();
    }

    private static byte[] FotoBranca()
    {
        using var b = new SkiaSharp.SKBitmap(800, 600);
        using (var c = new SkiaSharp.SKCanvas(b)) c.Clear(SkiaSharp.SKColors.White);
        using var img = SkiaSharp.SKImage.FromBitmap(b);
        return img.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80).ToArray();
    }
}
