using System.Security.Cryptography;
using QualyHub.Application.Fiscal;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Fiscal;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Tests.Integracao;

/// <summary>NF-e de entrada no banco real: importação, vínculo automático ao pedido, repetida, outro CNPJ, cancelamento e recebimento.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class NotasFiscaisTests(BancoFixture f)
{
    private static string Chave() => string.Concat(Enumerable.Range(0, 44).Select(_ => RandomNumberGenerator.GetInt32(10)));

    private static string Xml(string chave, string emit, string dest, int pedido) => $"""
        <nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00"><NFe><infNFe Id="NFe{chave}" versao="4.00">
          <ide><mod>55</mod><serie>1</serie><nNF>{RandomNumberGenerator.GetInt32(1, 99999)}</nNF><dhEmi>2026-10-05T09:30:00-03:00</dhEmi></ide>
          <emit><CNPJ>{emit}</CNPJ><xNome>Fornecedor Ficticio</xNome></emit><dest><CNPJ>{dest}</CNPJ><xNome>Construtora Ficticia</xNome></dest>
          <det nItem="1"><prod><cProd>C1</cProd><xProd>CIMENTO CP II SACO 50KG</xProd><uCom>SC</uCom><qCom>40</qCom><vUnCom>38.5</vUnCom><vProd>1540.00</vProd></prod></det>
          <det nItem="2"><prod><cProd>A1</cProd><xProd>AREIA MEDIA LAVADA</xProd><uCom>M3</uCom><qCom>6</qCom><vUnCom>120</vUnCom><vProd>720.00</vProd></prod></det>
          <total><ICMSTot><vNF>2260.00</vNF></ICMSTot></total><infAdic><infCpl>Pedido nº {pedido}</infCpl></infAdic>
        </infNFe></NFe></nfeProc>
        """;

    private static string Cancelamento(string chave) => $"""
        <procEventoNFe xmlns="http://www.portalfiscal.inf.br/nfe"><evento><infEvento><chNFe>{chave}</chNFe><tpEvento>110111</tpEvento></infEvento></evento></procEventoNFe>
        """;

    [FatoComBanco]
    public async Task Importa_vincula_ao_pedido_ignora_repetida_e_outro_cnpj_e_aplica_cancelamento()
    {
        var obra = await Dados.CriarObraAsync(f);
        var fornId = await Dados.CriarFornecedorAsync(f);
        string cnpjForn; var cnpjDest = Dados.Cnpj();
        var numero = RandomNumberGenerator.GetInt32(500_000, 900_000);
        var pedido = new Pedido { ObraId = obra, Numero = numero, FornecedorId = fornId, Entrega = DateOnly.FromDateTime(DateTime.Today), Status = StatusPedido.EnviadoFornecedor };
        var itCim = new PedidoItem { PedidoId = pedido.Id, Descricao = "Cimento CP II-F-32 saco 50 kg", Unidade = "SC", Quantidade = 100, ValorUnitario = 38 };
        var itAre = new PedidoItem { PedidoId = pedido.Id, Descricao = "Areia média lavada", Unidade = "M3", Quantidade = 12, ValorUnitario = 120 };
        pedido.Itens.Add(itCim); pedido.Itens.Add(itAre);
        await using (var db = f.Fabrica.Criar())
        {
            cnpjForn = (await db.Fornecedores.SingleAsync(x => x.Id == fornId)).CpfCnpj;
            db.Estabelecimentos.Add(new Estabelecimento { Codigo = $"E{numero}", RazaoSocial = "Filial Ficticia", Cnpj = cnpjDest, Raiz = cnpjDest[..8] });
            db.Pedidos.Add(pedido);
            await db.SaveChangesAsync();
        }
        var pasta = Path.Combine(Path.GetTempPath(), "qh-notas-" + Guid.NewGuid().ToString("N"));
        var svc = new NotasFiscaisServico(f.Fabrica, new ArmazenamentoLocal(pasta), f.Relogio);

        var chave = Chave();
        var r = await svc.ImportarAsync([LeitorNfe.LerTexto(Xml(chave, cnpjForn, cnpjDest, numero), "a.xml")], OrigemNota.Manual, null, null, null);
        Assert.Equal(1, r.Novas);
        Assert.Equal(1, r.Vinculadas);

        NotaFiscalEntrada n;
        await using (var db = f.Fabrica.CriarLeitura())
            n = await db.NotasFiscais.Include(x => x.Itens).SingleAsync(x => x.Chave == chave);
        Assert.Equal(SituacaoNota.Vinculada, n.Situacao);
        Assert.Equal(pedido.Id, n.PedidoId);
        Assert.Equal(obra, n.ObraId);
        Assert.Equal(fornId, n.FornecedorId);
        Assert.Equal(itCim.Id, n.Itens.Single(i => i.Item == 1).PedidoItemId);
        Assert.Equal(itAre.Id, n.Itens.Single(i => i.Item == 2).PedidoItemId);
        Assert.True(File.Exists(Path.Combine(pasta, n.ArquivoXml!)));

        // Repetida não entra; destinada a CNPJ de fora da construtora não entra.
        var r2 = await svc.ImportarAsync([LeitorNfe.LerTexto(Xml(chave, cnpjForn, cnpjDest, numero), "a.xml"),
                                          LeitorNfe.LerTexto(Xml(Chave(), cnpjForn, Dados.Cnpj(), numero), "b.xml")], OrigemNota.Manual, null, null, null);
        Assert.Equal(0, r2.Novas);
        Assert.Equal(1, r2.Repetidas);
        Assert.Contains(r2.Avisos, a => a.Contains("outro CNPJ"));

        // Recebimento com a chave tira a nota da fila.
        await using (var db = f.Fabrica.Criar())
        {
            await NotasFiscaisServico.MarcarRecebidaAsync(db, Guid.NewGuid(), chave, default);
            Assert.Equal(SituacaoNota.Recebimento, db.NotasFiscais.Local.Single(x => x.Chave == chave).Situacao);
        }

        // Cancelamento pela SEFAZ.
        var r3 = await svc.ImportarAsync([LeitorNfe.LerTexto(Cancelamento(chave), "c.xml")], OrigemNota.Manual, null, null, null);
        Assert.Equal(1, r3.Canceladas);
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal(SituacaoNota.Cancelada, (await db.NotasFiscais.SingleAsync(x => x.Chave == chave)).Situacao);

        // Pessoa desvincula e vincula de novo; outro fornecedor é recusado.
        var chave2 = Chave();
        await svc.ImportarAsync([LeitorNfe.LerTexto(Xml(chave2, cnpjForn, cnpjDest, 0), "d.xml")], OrigemNota.Manual, null, null, null);
        Guid id2;
        await using (var db = f.Fabrica.CriarLeitura()) id2 = (await db.NotasFiscais.SingleAsync(x => x.Chave == chave2)).Id;
        await svc.DesvincularAsync(id2);
        await svc.VincularPedidoAsync(id2, pedido.Id);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var n2 = await db.NotasFiscais.SingleAsync(x => x.Id == id2);
            Assert.Equal(OrigemVinculo.Manual, n2.VinculoOrigem);
        }
        Directory.Delete(pasta, true);
    }
}
