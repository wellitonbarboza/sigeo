using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Groundkey.Domain.Cadastros;
using Groundkey.Domain.Common;
using Groundkey.Domain.Documentos;
using Groundkey.Infrastructure.Importacao;
using Groundkey.Infrastructure.Servicos;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Groundkey.Tests.Integracao;

/// <summary>
/// Importação em lote dos documentos do SGQ (procedimento .docx + PDF) e das fichas FVS (.xlsx, aba BANCO DE DADOS):
/// documento entra com a revisão do arquivo em elaboração, ITS do serviço aponta para ele. Dados fictícios.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ImportacaoSgqLoteTests(BancoFixture f)
{
    private static byte[] Docx(string titulo)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            doc.PackageProperties.Title = titulo;
            var main = doc.AddMainDocumentPart();
            var hp = main.AddNewPart<HeaderPart>();
            hp.Header = new W.Header(new W.Paragraph(new W.Run(new W.Text("VERSÃO 01"))));
            main.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text("1. OBJETIVO"))),
                new W.SectionProperties(new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = main.GetIdOfPart(hp) })));
        }
        return ms.ToArray();
    }

    private static byte[] Ficha(string codigo, string referencia)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(LeitorDocumentoSgq.AbaFicha);
        string[][] campos = [["Identificação", codigo], ["Título do serviço", "SERVIÇO FICTÍCIO"], ["Revisão", "00"], ["Documento de referência", referencia], ["Amostragem", "100%"]];
        for (var i = 0; i < campos.Length; i++) { ws.Cell(2 + i, 2).Value = campos[i][0]; ws.Cell(2 + i, 3).Value = campos[i][1]; }
        ws.Cell(10, 2).Value = "Nº"; ws.Cell(10, 3).Value = "ITEM DE INSPEÇÃO"; ws.Cell(10, 4).Value = "MÉTODO DE INSPEÇÃO"; ws.Cell(10, 5).Value = "CRITÉRIO DE AVALIAÇÃO";
        ws.Cell(11, 2).Value = 1; ws.Cell(11, 3).Value = "Prumo"; ws.Cell(11, 4).Value = "Prumo"; ws.Cell(11, 5).Value = "≤ 5 mm";
        ws.Cell(12, 2).Value = 2; ws.Cell(12, 3).Value = "Limpeza"; ws.Cell(12, 4).Value = "Visual"; ws.Cell(12, 5).Value = "Sem detritos";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static ArquivoLoteSgq Arq(string nome, byte[] bytes) => new(nome, bytes, LeitorDocumentoSgq.Identificar(new MemoryStream(bytes), nome));

    [FatoComBanco]
    public async Task Lote_cria_documento_em_elaboracao_e_liga_a_its_da_fvs()
    {
        var n = Random.Shared.Next(100, 999);
        var cod = $"PEIS.{n}";
        await using (var db0 = f.Fabrica.CriarLeitura())
            if (await db0.Documentos.AnyAsync(d => d.Codigo == cod)) return; // número já sorteado em outra execução
        var armazenamento = new ArmazenamentoLocal(Path.Combine(Path.GetTempPath(), "qh-sgq-" + Guid.NewGuid().ToString("N")));
        var docs = new DocumentosServico(f.Fabrica, f.Usuario, armazenamento, f.Relogio);
        var pasta = await docs.CriarPastaAsync(new Pasta { Nome = $"ITS teste {Dados.Sufixo()}" });
        var tipos = Groundkey.Application.Carga.CargaPadrao.TiposDocumentoSgq;

        // Nome do arquivo com outro número: vale o título do arquivo.
        List<ArquivoLoteSgq> arquivos = [Arq("PEIS.01-Servico_ficticio.REV01.docx", Docx($"{cod} — Serviço fictício")), Arq($"{cod} - Serviço fictício.pdf", "%PDF-1.4 ficticio"u8.ToArray())];
        var previa = await docs.AnalisarLoteAsync(pasta, null, tipos, arquivos);
        var item = Assert.Single(previa.Itens);
        Assert.Equal(AcaoLoteSgq.Criar, item.Acao);
        Assert.Equal(cod, item.Codigo);
        Assert.Equal(1, item.Revisao);
        Assert.StartsWith("ITS", item.Tipo);
        await docs.ImportarLoteAsync(pasta, null, tipos, arquivos);

        await using (var db = f.Fabrica.CriarLeitura())
        {
            var d = await db.Documentos.Include(x => x.Revisoes).SingleAsync(x => x.Codigo == cod);
            Assert.Equal("Serviço fictício", d.Titulo);
            Assert.Equal(StatusDocumentoSgq.EmElaboracao, d.Status);
            Assert.Null(d.RevisaoVigenteId); // nada vigente sem aprovação
            var r = Assert.Single(d.Revisoes);
            Assert.Equal(1, r.Revisao);
            Assert.Equal(StatusRevisao.EmElaboracao, r.Status);
            Assert.NotNull(r.ArquivoEditavelBlob);
            Assert.NotNull(r.HashSha256);
            Assert.EndsWith("R01.pdf", r.ArquivoPublicadoNome);
        }

        // Segunda vez: revisão já em andamento com os arquivos → ignora.
        Assert.Equal(AcaoLoteSgq.Ignorar, Assert.Single((await docs.AnalisarLoteAsync(pasta, null, tipos, arquivos)).Itens).Acao);

        // Ficha FVS da empresa (aba BANCO DE DADOS) → modelo; ITS criada com o código da empresa e ligada ao documento.
        var kit = LeitorKitModelos.LerVarios([($"FVS-01_Ficticio.xlsx", Ficha($"FVS-{n}", $"{cod} — Serviço fictício REV01"))]);
        var kitSvc = new KitModelosServico(f.Fabrica, new QualidadeObraServico(f.Fabrica, f.Usuario, f.Relogio));
        var res = await kitSvc.AplicarAsync(kit);
        Assert.Contains(res.ItsCriadas, x => x.StartsWith(cod));
        Assert.Contains(cod, res.DocumentosVinculados);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var doc = await db.Documentos.SingleAsync(x => x.Codigo == cod);
            var srv = await db.ServicosControlados.SingleAsync(s => s.Codigo == cod);
            Assert.Equal(doc.Id, srv.DocumentoItsId);
            var m = await db.ModelosInspecao.Include(x => x.Itens).SingleAsync(x => x.Codigo == $"FVS-{n}");
            Assert.Equal(srv.Id, m.ServicoControladoId);
            Assert.Equal(2, m.Itens.Count);
            Assert.Equal("Serviço fictício", m.Titulo);
        }
    }

    [FatoComBanco]
    public async Task Kit_com_materiais_atualiza_cadastro_e_idf_da_entrega_entra_na_avaliacao_trimestral()
    {
        var suf = Dados.Sufixo();
        var nome = $"Material ficticio {suf}";
        var kit = new KitModelos(
            [new KitModelo(1, Groundkey.Domain.Common.TipoModelo.FVM, $"FVM-X{suf}", nome, 0, null, null, "99", nome, "NBR ficticia", "Toda entrega", "No recebimento", null,
                false, false, true, "Armazenamento: coberto", true)],
            [new KitItem(1, $"FVM-X{suf}", 1, "Embalagem", "Visual", "Íntegra", false, Groundkey.Domain.Common.FotoItem.SoEmNaoConformidade, true, null, null)], [])
        {
            Materiais = [new KitMaterialControlado("99", nome, "NBR ficticia", "NF; ficha técnica", null, "Local coberto", "A ou C", "PSQ ou histórico", "PEIS.01")]
        };
        // Material antigo com o código da lista oficial: cede o código (vira -ANT).
        var codigo = $"Z{suf}"[..Math.Min(9, suf.Length + 1)];
        kit = kit with { Materiais = [kit.Materiais[0] with { Codigo = codigo }] };
        await using (var db = f.Fabrica.Criar()) { db.MateriaisControlados.Add(new MaterialControlado { Codigo = codigo, Nome = $"Generico {suf}" }); await db.SaveChangesAsync(); }
        var kitSvc = new KitModelosServico(f.Fabrica, new QualidadeObraServico(f.Fabrica, f.Usuario, f.Relogio));
        var res = await kitSvc.AplicarAsync(kit);
        Assert.Contains(res.MateriaisCriados, x => x == $"{codigo} – {nome}");
        Assert.Contains(res.CodigosLiberados, x => x.EndsWith($"{codigo}-ANT"));
        await using (var db = f.Fabrica.CriarLeitura())
            Assert.Equal($"{codigo}-ANT", (await db.MateriaisControlados.SingleAsync(x => x.Nome == $"Generico {suf}")).Codigo);
        Guid matId;
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var m = await db.MateriaisControlados.SingleAsync(x => x.Nome == nome);
            matId = m.Id;
            Assert.Equal(("A ou C", "Local coberto", "NBR ficticia", "PEIS.01"), (m.FormasQualificacao, m.Armazenamento, m.Norma, m.ServicosQueUtilizam));
            var modelo = await db.ModelosInspecao.SingleAsync(x => x.Codigo == $"FVM-X{suf}");
            Assert.Equal(m.Id, modelo.MaterialControladoId);
            Assert.True(modelo.RastrearAplicacao);
        }
        // Segunda importação: mesmo material (pelo nome) é atualizado, não duplicado.
        var segunda = await kitSvc.AplicarAsync(kit with { } );
        Assert.Contains(segunda.MateriaisAtualizados, x => x.EndsWith(nome));

        // Entrega com notas: IDF = 0,4×9 + 0,4×8 + 0,2×10 = 8,8 (pesos padrão).
        var obra = await Dados.CriarObraAsync(f);
        var forn = await Dados.CriarFornecedorAsync(f);
        var prod = await Dados.CriarProdutoAsync(f, $"Produto ficticio {suf}", 10m);
        var pedido = new Groundkey.Domain.Suprimentos.Pedido { ObraId = obra, Numero = Random.Shared.Next(100_000, 900_000), FornecedorId = forn, Entrega = DateOnly.FromDateTime(DateTime.Today), Status = Groundkey.Domain.Common.StatusPedido.EnviadoFornecedor };
        var pi = new Groundkey.Domain.Suprimentos.PedidoItem { PedidoId = pedido.Id, ProdutoId = prod.Id, Descricao = prod.Nome, Unidade = "UN", Quantidade = 10, ValorUnitario = 10 };
        pedido.Itens.Add(pi);
        await using (var db = f.Fabrica.Criar()) { db.Pedidos.Add(pedido); await db.SaveChangesAsync(); }
        var r = new Groundkey.Domain.Suprimentos.Recebimento
        {
            PedidoId = pedido.Id, NumeroNotaFiscal = "3001", DataRecebimento = DateTimeOffset.Now, NotaPrazo = 9, NotaQualidade = 8, NotaDocumentacao = 10,
            Itens = [new Groundkey.Domain.Suprimentos.RecebimentoItem { PedidoItemId = pi.Id, Descricao = pi.Descricao, Unidade = "UN", QtdPedida = 10, QtdRecebida = 10 }]
        };
        await new RecebimentosServico(f.Fabrica, f.Usuario, f.Relogio).RegistrarRecebimentoAsync(r);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var salvo = await db.Recebimentos.SingleAsync(x => x.Id == r.Id);
            Assert.Equal(8.8m, salvo.Idf);
            Assert.True(salvo.FvmAprovada);
        }
        var periodo = Groundkey.Domain.Suprimentos.AvaliacaoSemestral.PeriodoDe(DateOnly.FromDateTime(DateTime.Today), 3);
        await new AvaliacaoFornecedoresServico(f.Fabrica, f.Usuario, f.Relogio).ConsolidarSemestreAsync(periodo);
        await using (var db = f.Fabrica.CriarLeitura())
        {
            var a = await db.AvaliacoesSemestrais.SingleAsync(x => x.Periodo == periodo && x.FornecedorId == forn);
            Assert.Equal(8.8m, a.IdfMedio);
            Assert.Equal(1, a.QtdEntregasIdf);
        }
    }
}
