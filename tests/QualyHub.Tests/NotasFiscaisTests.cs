using System.IO.Compression;
using System.Text;
using QualyHub.Application.Fiscal;
using QualyHub.Domain.Fiscal;
using QualyHub.Infrastructure.Servicos;

namespace QualyHub.Tests;

/// <summary>Leitura do XML da NF-e, cancelamento, números de pedido citados e pareamento nota × pedido (dados fictícios).</summary>
public class NotasFiscaisTests
{
    // Chave fictícia (44 dígitos), CNPJs fictícios.
    private const string Chave = "41261012345678000195550010000012341000012345";

    public static string XmlNota(string chave = Chave, string infCpl = "REF. PEDIDO Nº 57 - OBRA TESTE", string xPed = "57") => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <nfeProc xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
          <NFe><infNFe Id="NFe{chave}" versao="4.00">
            <ide><mod>55</mod><serie>1</serie><nNF>1234</nNF><dhEmi>2026-10-05T09:30:00-03:00</dhEmi></ide>
            <emit><CNPJ>12345678000195</CNPJ><xNome>Fornecedor Ficticio Materiais Ltda</xNome></emit>
            <dest><CNPJ>11444777000161</CNPJ><xNome>Construtora Exemplo</xNome></dest>
            <det nItem="1"><prod><cProd>CIM50</cProd><xProd>CIMENTO CP II-F 32 SACO 50KG</xProd><NCM>25232910</NCM><CFOP>5102</CFOP><uCom>SC</uCom>
              <qCom>100.0000</qCom><vUnCom>38.5000000000</vUnCom><vProd>3850.00</vProd><xPed>{xPed}</xPed><nItemPed>1</nItemPed></prod></det>
            <det nItem="2"><prod><cProd>AR01</cProd><xProd>AREIA MEDIA LAVADA</xProd><NCM>25059000</NCM><CFOP>5102</CFOP><uCom>M3</uCom>
              <qCom>6.0000</qCom><vUnCom>120.00</vUnCom><vProd>720.00</vProd></prod></det>
            <total><ICMSTot><vFrete>150.00</vFrete><vNF>4720.00</vNF></ICMSTot></total>
            <infAdic><infCpl>{infCpl}</infCpl></infAdic>
          </infNFe></NFe>
          <protNFe versao="4.00"><infProt><chNFe>{chave}</chNFe><cStat>100</cStat></infProt></protNFe>
        </nfeProc>
        """;

    private static string XmlCancelamento(string chave = Chave) => $"""
        <procEventoNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="1.00"><evento versao="1.00"><infEvento Id="ID110111{chave}01">
          <chNFe>{chave}</chNFe><tpEvento>110111</tpEvento><nSeqEvento>1</nSeqEvento></infEvento></evento></procEventoNFe>
        """;

    [Fact]
    public void Le_nota_completa()
    {
        var r = Assert.IsType<NotaLida>(LeitorNfe.LerTexto(XmlNota(), "a.xml"));
        var n = r.Nota;
        Assert.Equal(Chave, n.Chave);
        Assert.Equal(1234, n.Numero);
        Assert.Equal("12345678000195", n.EmitenteCnpj);
        Assert.Equal("11444777000161", n.DestinatarioCnpj);
        Assert.Equal(4720m, n.ValorTotal);
        Assert.Equal(150m, n.ValorFrete);
        Assert.Equal(2, n.Itens.Count);
        var cimento = n.Itens.Single(i => i.Item == 1);
        Assert.Equal(100m, cimento.Quantidade);
        Assert.Equal(38.5m, cimento.ValorUnitario);
        Assert.Equal("57", cimento.PedidoCitado);
        Assert.Equal("57", n.PedidosCitados);
        Assert.Equal(new DateTimeOffset(2026, 10, 5, 9, 30, 0, TimeSpan.FromHours(-3)), n.Emissao);
    }

    [Fact]
    public void Le_cancelamento_e_ignora_lixo()
    {
        Assert.Equal(Chave, Assert.IsType<CancelamentoLido>(LeitorNfe.LerTexto(XmlCancelamento(), "c.xml")).Chave);
        Assert.IsType<XmlIgnorado>(LeitorNfe.LerTexto("<html>não é nota</html>", "x.xml"));
        Assert.IsType<XmlIgnorado>(LeitorNfe.LerTexto("isto não é xml", "y.xml"));
        // DTD/entidade externa é recusada (XXE)
        Assert.IsType<XmlIgnorado>(LeitorNfe.LerTexto("<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]><x>&e;</x>", "z.xml"));
        // NFC-e (modelo 65) não é compra
        Assert.IsType<XmlIgnorado>(LeitorNfe.LerTexto(XmlNota().Replace("<mod>55</mod>", "<mod>65</mod>"), "nfce.xml"));
    }

    [Fact]
    public void Le_zip_com_varios_xml()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (nome, conteudo) in new[] { ("n1.xml", XmlNota()), ("c1.xml", XmlCancelamento()), ("leia.txt", "ignorar") })
            {
                using var w = new StreamWriter(zip.CreateEntry(nome).Open(), Encoding.UTF8);
                w.Write(conteudo);
            }
        }
        ms.Position = 0;
        var lidos = LeitorNfe.Ler(ms, "lote.zip");
        Assert.Equal(2, lidos.Count);
        Assert.Single(lidos.OfType<NotaLida>());
        Assert.Single(lidos.OfType<CancelamentoLido>());
        // Resposta do SIEG em ZIP
        Assert.Equal(2, FonteSieg.LerResposta(ms.ToArray()).Count);
    }

    [Fact]
    public void Resposta_sieg_em_json_base64()
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(XmlNota()));
        var r = FonteSieg.LerResposta(Encoding.UTF8.GetBytes($"{{\"xmls\":[\"{b64}\"]}}"));
        Assert.Single(r);
        Assert.IsType<NotaLida>(LeitorNfe.LerTexto(r[0].Item2, r[0].Item1));
    }

    [Theory]
    [InlineData("Pedido de compra nº 812, entrega obra", "", "812")]
    [InlineData("PEDIDO: 77 / 78", "", "77")]
    [InlineData("sem referência", "PC-0045", "45")]
    [InlineData("", "", "")]
    public void Pedidos_citados(string infCpl, string xPed, string esperado)
    {
        var n = Assert.IsType<NotaLida>(LeitorNfe.LerTexto(XmlNota(infCpl: infCpl, xPed: xPed), "a.xml")).Nota;
        Assert.Equal(esperado, string.Join(",", LeitorNfe.PedidosCitados(n).Take(1)));
    }

    [Fact]
    public void Pareia_itens_pela_descricao_unidade_e_saldo()
    {
        var n = Assert.IsType<NotaLida>(LeitorNfe.LerTexto(XmlNota(), "a.xml")).Nota;
        var cimento = Guid.NewGuid(); var areia = Guid.NewGuid(); var brita = Guid.NewGuid();
        var pedido = new List<ItemPedidoParaConferencia>
        {
            new(areia, "Areia média lavada", "M3", 10, null),
            new(cimento, "Cimento CP II-F-32 saco 50 kg", "SC", 100, null),
            new(brita, "Brita 1", "M3", 5, null)
        };
        var pares = ConferenciaNota.Parear(n.Itens.ToList(), pedido);
        Assert.Equal(cimento, pares[1].PedidoItemId);
        Assert.Equal(areia, pares[2].PedidoItemId);
        Assert.True(pares[1].Confianca >= ConferenciaNota.ConfiancaMinima);
        // Item do pedido não se repete: dois itens de nota iguais não caem no mesmo item do pedido
        Assert.Equal(2, pares.Values.Select(p => p.PedidoItemId).Distinct().Count());
    }

    [Fact]
    public void Nota_vinculada_desvincula_e_recebida_nao_muda()
    {
        var n = new NotaFiscalEntrada();
        n.VincularPedido(Guid.NewGuid(), Guid.NewGuid(), OrigemVinculo.Regra, "teste");
        Assert.Equal(SituacaoNota.Vinculada, n.Situacao);
        n.Desvincular("x");
        Assert.Equal(SituacaoNota.Recebida, n.Situacao);
        n.Situacao = SituacaoNota.Recebimento;
        Assert.Throws<InvalidOperationException>(() => n.VincularPedido(Guid.NewGuid(), Guid.NewGuid(), OrigemVinculo.Manual, "x"));
    }
}
