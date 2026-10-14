using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Infrastructure.Importacao;
using QualyHub.Infrastructure.Servicos;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace QualyHub.Tests.Integracao;

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
        var tipos = QualyHub.Application.Carga.CargaPadrao.TiposDocumentoSgq;

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
}
