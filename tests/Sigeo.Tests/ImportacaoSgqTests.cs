using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Sigeo.Domain.Common;
using Sigeo.Infrastructure.Importacao;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Sigeo.Tests;

/// <summary>Leitura dos arquivos do SGQ da empresa (procedimento .docx e ficha FVS .xlsx) para importação. Dados fictícios.</summary>
public class ImportacaoSgqTests
{
    internal static byte[] DocxExemplo(string? tituloPropriedade, string cabecalho, int revisaoHistorico, string descricao)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            if (tituloPropriedade is not null) doc.PackageProperties.Title = tituloPropriedade;
            var main = doc.AddMainDocumentPart();
            var hp = main.AddNewPart<HeaderPart>();
            hp.Header = new W.Header(new W.Table(new W.TableRow(
                Celula("EMPRESA EXEMPLO"), Celula(cabecalho.Split('|')[0]), Celula(cabecalho.Split('|')[1]))));
            var rid = main.GetIdOfPart(hp);
            main.Document = new W.Document(new W.Body(
                new W.Paragraph(new W.Run(new W.Text("1. OBJETIVO"))),
                new W.Table(
                    new W.TableRow(Celula("Rev."), Celula("Data"), Celula("Descrição")),
                    new W.TableRow(Celula("00"), Celula("—"), Celula("Emissão")),
                    new W.TableRow(Celula(revisaoHistorico.ToString("00")), Celula("01/01/2026"), Celula(descricao))),
                new W.SectionProperties(new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = rid })));
        }
        return ms.ToArray();
    }

    private static W.TableCell Celula(string t) => new(new W.Paragraph(new W.Run(new W.Text(t))));

    internal static byte[] FichaExemplo(string identificacao, string referencia)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(LeitorDocumentoSgq.AbaFicha);
        wb.AddWorksheet("FVS - Locais 01 a 08");
        ws.Cell(1, 2).Value = "EMPRESA EXEMPLO — SISTEMA DE GESTÃO DA QUALIDADE";
        string[][] campos =
        [
            ["Obra", "Obra Exemplo"], ["Identificação", identificacao], ["Título do serviço", "EXECUÇÃO DE SERVIÇO EXEMPLO"], ["Revisão", "02"],
            ["Documento de referência", referencia], ["Regime de execução (DS.03)", "Terceiro"], ["Momento da inspeção", "Após a execução"],
            ["Unidade de inspeção (local)", "Pavimento"], ["Amostragem", "100% dos ambientes"]
        ];
        for (var i = 0; i < campos.Length; i++) { ws.Cell(3 + i, 2).Value = campos[i][0]; ws.Cell(3 + i, 3).Value = campos[i][1]; }
        ws.Cell(14, 2).Value = "PRÉ-REQUISITOS PARA A EXECUÇÃO DO SERVIÇO";
        ws.Cell(15, 2).Value = "• Projeto na revisão vigente\n• Base limpa";
        ws.Cell(17, 2).Value = "Nº"; ws.Cell(17, 3).Value = "ITEM DE INSPEÇÃO"; ws.Cell(17, 4).Value = "MÉTODO DE INSPEÇÃO"; ws.Cell(17, 5).Value = "CRITÉRIO DE AVALIAÇÃO / TOLERÂNCIA";
        ws.Cell(18, 2).Value = 1; ws.Cell(18, 3).Value = "Prumo"; ws.Cell(18, 4).Value = "Prumo de face"; ws.Cell(18, 5).Value = "≤ 5 mm";
        ws.Cell(19, 2).Value = 2; ws.Cell(19, 3).Value = "Limpeza"; ws.Cell(19, 4).Value = "Visual"; ws.Cell(19, 5).Value = "Sem detritos";
        ws.Cell(20, 2).Value = 3; ws.Cell(20, 3).Value = "Nível"; ws.Cell(20, 4).Value = "Nível a laser"; ws.Cell(20, 5).Value = "± 3 mm";
        ws.Cell(21, 2).Value = 4; ws.Cell(21, 3).Value = "Linhas reservas: preencher para incluir itens.";
        ws.Cell(23, 2).Value = "Referências: procedimento do serviço.";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Docx_usa_propriedades_cabecalho_e_historico()
    {
        var bytes = DocxExemplo("PEIS.07 — Serviço de exemplo", "IDENTIFICAÇÃO PEIS.07|VERSÃO 02", 2, "Reformulação para o padrão da empresa");
        // O nome do arquivo tem outro número: vale o conteúdo.
        var id = LeitorDocumentoSgq.Identificar(new MemoryStream(bytes), "PEIS.01-Servico_de_exemplo.Empresa.REV01.docx");
        Assert.Equal("PEIS.07", id.Codigo);
        Assert.Equal("Serviço de exemplo", id.Titulo);
        Assert.Equal(2, id.Revisao);
        Assert.Equal("Reformulação para o padrão da empresa", id.DescricaoRevisao);

        var semProp = LeitorDocumentoSgq.Identificar(new MemoryStream(DocxExemplo(null, "IDENTIFICAÇÃO PEIS.09|VERSÃO 01", 1, "x")), "Qualquer.docx");
        Assert.Equal("PEIS.09", semProp.Codigo);
        Assert.Equal(1, semProp.Revisao);
    }

    [Fact]
    public void Nome_do_arquivo_e_fallback()
    {
        var (cod, rev, tit) = LeitorDocumentoSgq.PeloNome("PEIS.01-Montagem_de_forma.Empresa.REV01.docx");
        Assert.Equal("PEIS.01", cod);
        Assert.Equal(1, rev);
        Assert.Equal("Montagem de forma", tit);
        var lixo = LeitorDocumentoSgq.Identificar(new MemoryStream([1, 2, 3]), "FVS-12_Pintura.REV00.docx");
        Assert.Equal("FVS-12", lixo.Codigo);
        Assert.Equal(0, lixo.Revisao);
        Assert.Equal("PEIS.33", LeitorDocumentoSgq.CodigoNoInicio("PEIS.33 — Impermeabilização REV02"));
        Assert.Equal("Impermeabilização", LeitorDocumentoSgq.NomeDepoisDoCodigo("PEIS.33 — Impermeabilização REV02"));
    }

    [Fact]
    public void Ficha_fvs_vira_modelo_com_itens_e_equipamento()
    {
        var bytes = FichaExemplo("FVS-07", "PEIS.07 — Execução de serviço exemplo REV01");
        var (m, itens) = LeitorDocumentoSgq.LerFichaFvs(new MemoryStream(bytes), "FVS-01_Exemplo.xlsx", 1)!.Value;
        Assert.Equal("FVS-07", m.Codigo);
        Assert.Equal(TipoModelo.FVS, m.Tipo);
        Assert.Equal("Execução de serviço exemplo", m.Titulo); // caixa e acentos do documento de referência
        Assert.Equal("PEIS.07", m.Its);
        Assert.Equal("Execução de serviço exemplo", m.NomeIts);
        Assert.Equal(2, m.Revisao);
        Assert.Equal("Após a execução", m.Momento);
        Assert.Equal("Pavimento", m.NivelLocal);
        Assert.Null(LeitorDocumentoSgq.NivelDoLocal("Pavimento / peça (pilar, viga, laje)"));
        Assert.Equal("Apartamento", LeitorDocumentoSgq.NivelDoLocal("Unidade"));
        Assert.Equal("100% dos ambientes", m.Amostragem);
        Assert.Contains("Base limpa", m.Orientacoes);
        Assert.Contains("Regime de execução: Terceiro", m.Orientacoes);
        Assert.Equal(3, itens.Count);
        Assert.True(itens[0].ExigeEquipamento);
        Assert.False(itens[1].ExigeEquipamento);
        Assert.True(itens[2].ExigeEquipamento);
        Assert.All(itens, i => Assert.Equal("FVS-07", i.Modelo));

        var kit = LeitorKitModelos.LerVarios([("a.xlsx", bytes), ("b.xlsx", FichaExemplo("FVS-08", "PEIS.08 — Outro REV01"))]);
        Assert.Equal(2, kit.Modelos.Count);
        Assert.Equal(6, kit.Itens.Count);
        Assert.Empty(KitModelosServico.Validar(kit));
    }

    [Theory]
    [InlineData("PEIS.03", "PEIS.03")]
    [InlineData("ITS 03.02", "ITS 03.02")]
    [InlineData("03.02", "ITS 03.02")]
    [InlineData("pes-12", "PES-12")]
    public void Codigo_da_its_criada_mantem_padrao_da_empresa(string its, string esperado) =>
        Assert.Equal(esperado, KitModelosServico.CodigoIts(its));

    [Fact]
    public void Tipo_pelo_codigo()
    {
        var tipos = Sigeo.Application.Carga.CargaPadrao.TiposDocumentoSgq;
        Assert.StartsWith("ITS", DocumentosServico.TipoPorCodigo("PEIS.03", tipos));
        Assert.StartsWith("FVS", DocumentosServico.TipoPorCodigo("FVS-03", tipos));
        Assert.StartsWith("F /", DocumentosServico.TipoPorCodigo("FORM.67", tipos));
        Assert.Null(DocumentosServico.TipoPorCodigo("PO.19", tipos));
    }

    [Fact]
    public void Pdf_pelo_titulo_ou_pareado_com_o_editavel()
    {
        var hex = Convert.ToHexString([0xFE, 0xFF, .. System.Text.Encoding.BigEndianUnicode.GetBytes("PEIS.07 — Serviço de exemplo")]);
        var pdf = System.Text.Encoding.Latin1.GetBytes($"%PDF-1.4\n1 0 obj << /Title <{hex}> /Producer (x) >> endobj");
        Assert.Equal("PEIS.07 — Serviço de exemplo", LeitorDocumentoSgq.TituloPdf(new MemoryStream(pdf)));
        Assert.Equal("PEIS.07", LeitorDocumentoSgq.Identificar(new MemoryStream(pdf), "PEIS.01-Servico.REV01.pdf").Codigo);
        Assert.Equal("Outro", LeitorDocumentoSgq.TituloPdf(new MemoryStream("%PDF-1.4 << /Title (Outro) >>"u8.ToArray())));

        // Sem título no PDF: assume a identificação do editável de mesmo nome.
        var docx = new ArquivoLoteSgq("PEIS.01-Servico.REV01.docx", [], new IdentificacaoArquivoSgq("PEIS.01-Servico.REV01.docx", "PEIS.07", "Serviço", 1, "desc", "propriedades"));
        var semTitulo = new ArquivoLoteSgq("PEIS.01-Servico.REV01.pdf", [], new IdentificacaoArquivoSgq("PEIS.01-Servico.REV01.pdf", "PEIS.01", "Servico", 1, null, "nome do arquivo"));
        var par = DocumentosServico.ParearPdfs([docx, semTitulo]);
        Assert.Equal("PEIS.07", par[1].Identificacao.Codigo);
        Assert.Equal("PEIS.01-Servico.REV01.pdf", par[1].Identificacao.Arquivo);
    }

    internal static byte[] FvmExemplo(string identificacao, string material)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(LeitorDocumentoSgq.AbaFicha);
        string[][] campos =
        [
            ["Obra", "Obra Exemplo"], ["Identificação", identificacao], ["Material", material], ["Revisão", "00"],
            ["Norma / especificação", "ABNT NBR 0000:2020"], ["Documentos exigidos do fornecedor", "NF; ficha técnica"], ["Ensaio", "—"],
            ["Armazenamento (DS.12)", "Local coberto, sobre estrado"], ["Serviços que utilizam", "PEIS.01 · PEIS.02"]
        ];
        for (var i = 0; i < campos.Length; i++) { ws.Cell(3 + i, 2).Value = campos[i][0]; ws.Cell(3 + i, 3).Value = campos[i][1]; }
        ws.Cell(14, 2).Value = "Nº"; ws.Cell(14, 3).Value = "ITEM DE VERIFICAÇÃO"; ws.Cell(14, 4).Value = "MÉTODO"; ws.Cell(14, 5).Value = "CRITÉRIO DE ACEITAÇÃO / TOLERÂNCIA"; ws.Cell(14, 6).Value = "AMOSTRAGEM";
        ws.Cell(15, 2).Value = 1; ws.Cell(15, 3).Value = "NF × pedido"; ws.Cell(15, 4).Value = "Conferência documental"; ws.Cell(15, 5).Value = "Igual ao pedido"; ws.Cell(15, 6).Value = "Toda entrega";
        ws.Cell(16, 2).Value = 2; ws.Cell(16, 3).Value = "Dimensões"; ws.Cell(16, 4).Value = "Trena"; ws.Cell(16, 5).Value = "± 3 mm"; ws.Cell(16, 6).Value = "10 peças por lote";
        ws.Cell(17, 2).Value = 3;
        ws.Cell(19, 2).Value = "Referências: lista de materiais.";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    internal static byte[] RequisitosExemplo()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Lista").Cell(1, 1).Value = "Lista de fornecedores";
        var ws = wb.AddWorksheet("Requisitos por grupo");
        ws.Cell(2, 1).Value = "REQUISITOS";
        string[] cab = ["Grupo", "Material", "Documentos exigidos do fornecedor", "Forma de qualificação", "Critério de qualificação", "Registro de recebimento"];
        for (var c = 0; c < cab.Length; c++) ws.Cell(4, c + 1).Value = cab[c];
        ws.Cell(5, 1).Value = "01"; ws.Cell(5, 2).Value = "Tubos de PVC exemplo"; ws.Cell(5, 3).Value = "NF"; ws.Cell(5, 4).Value = "A ou B"; ws.Cell(5, 5).Value = "Marca no PSQ"; ws.Cell(5, 6).Value = "FVM-01 · PEIS.01";
        ws.Cell(6, 1).Value = "02"; ws.Cell(6, 2).Value = "Concreto exemplo"; ws.Cell(6, 3).Value = "NF por caminhão"; ws.Cell(6, 4).Value = "D"; ws.Cell(6, 6).Value = "FORM.09 · PEIS.05";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Ficha_fvm_e_lista_de_requisitos_viram_materiais_e_modelo()
    {
        var kit = LeitorKitModelos.LerVarios([("FVM-01.xlsx", FvmExemplo("FVM-01", "TUBOS DE PVC EXEMPLO")), ("FORM.76.xlsx", RequisitosExemplo())]);
        var m = Assert.Single(kit.Modelos);
        Assert.Equal(TipoModelo.FVM, m.Tipo);
        Assert.Equal("01", m.Material);
        Assert.Equal("Tubos de PVC exemplo", m.NomeMaterial); // nome oficial da lista (sigla mantida)
        Assert.Equal("Tubos de PVC exemplo", m.Titulo);
        Assert.Equal("ABNT NBR 0000:2020", m.Norma);
        Assert.Equal("Toda entrega; 10 peças por lote", m.Amostragem);
        Assert.Contains("Armazenamento: Local coberto", m.Orientacoes);
        Assert.Equal(2, kit.Itens.Count);
        Assert.EndsWith("Amostragem: 10 peças por lote", kit.Itens[1].Criterio);
        Assert.True(kit.Itens[1].ExigeEquipamento);
        Assert.Equal(2, kit.Materiais.Count);
        var tubo = kit.Materiais.Single(x => x.Codigo == "01");
        Assert.Equal("A ou B", tubo.FormasQualificacao);
        Assert.Equal("NF", tubo.DocumentosExigidos); // a lista prevalece; a ficha completa o que falta
        Assert.Equal("Local coberto, sobre estrado", tubo.Armazenamento);
        Assert.Null(tubo.Ensaios); // "—" não é ensaio
        Assert.Equal("PEIS.05", kit.Materiais.Single(x => x.Codigo == "02").ServicosQueUtilizam);
        Assert.Empty(KitModelosServico.Validar(kit));
        Assert.Equal("Tubos e conexões de PVC", LeitorDocumentoSgq.Frase("TUBOS E CONEXÕES DE PVC"));
        Assert.Equal("Aço para armaduras", LeitorDocumentoSgq.Frase("AÇO PARA ARMADURAS"));
    }

    [Fact]
    public void Kit_com_aba_materiais()
    {
        using var wb = new XLWorkbook();
        var mat = wb.AddWorksheet("Materiais");
        string[] cm = ["Código", "Nome", "Norma", "Documentos exigidos", "Ensaios", "Armazenamento", "Formas de qualificação", "Critério de qualificação", "Serviços que utilizam"];
        for (var c = 0; c < cm.Length; c++) mat.Cell(1, c + 1).Value = cm[c];
        mat.Cell(2, 1).Value = "07"; mat.Cell(2, 2).Value = "Material exemplo"; mat.Cell(2, 3).Value = "NBR X"; mat.Cell(2, 7).Value = "B";
        var mod = wb.AddWorksheet("Modelos");
        string[] cmo = ["Tipo", "Código", "Título", "Revisão", "Material", "Nome do material", "Norma / especificação", "Amostragem"];
        for (var c = 0; c < cmo.Length; c++) mod.Cell(1, c + 1).Value = cmo[c];
        mod.Cell(2, 1).Value = "FVM"; mod.Cell(2, 2).Value = "FVM-07"; mod.Cell(2, 3).Value = "Material exemplo"; mod.Cell(2, 5).Value = "07"; mod.Cell(2, 6).Value = "Material exemplo"; mod.Cell(2, 7).Value = "NBR X"; mod.Cell(2, 8).Value = "Toda entrega";
        var it = wb.AddWorksheet("Itens");
        string[] ci = ["Modelo", "Ordem", "Item de verificação", "Método", "Critério"];
        for (var c = 0; c < ci.Length; c++) it.Cell(1, c + 1).Value = ci[c];
        it.Cell(2, 1).Value = "FVM-07"; it.Cell(2, 2).Value = 1; it.Cell(2, 3).Value = "Embalagem"; it.Cell(2, 4).Value = "Visual"; it.Cell(2, 5).Value = "Íntegra";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var kit = LeitorKitModelos.LerVarios([("kit.xlsx", ms.ToArray())]);
        var m = Assert.Single(kit.Materiais);
        Assert.Equal(("07", "B", "NBR X"), (m.Codigo, m.FormasQualificacao, m.Norma));
        Assert.Single(kit.Modelos);
    }
}

/// <summary>IDF da entrega (notas 0–10 com pesos) e período trimestral da avaliação de fornecedores.</summary>
public class AvaliacaoIdfTests
{
    [Fact]
    public void Notas_definem_criterios_e_idf_ponderado()
    {
        var r = new Sigeo.Domain.Suprimentos.Recebimento { NotaPrazo = 10, NotaQualidade = 8, NotaDocumentacao = 5 };
        r.AplicarNotas(0.4m, 0.4m, 0.2m, 6m);
        Assert.Equal(8.2m, r.Idf); // 0,4×10 + 0,4×8 + 0,2×5
        Assert.Equal(ResultadoCriterio.Aprovado, r.FvmPrazo);
        Assert.Equal(ResultadoCriterio.Reprovado, r.FvmAtendimento); // 5 < mínimo 6
        Assert.False(r.FvmAprovada);
        Assert.Equal(8.2m, r.NotaEntrega);

        var parcial = new Sigeo.Domain.Suprimentos.Recebimento { NotaPrazo = 9, FvmQualidade = ResultadoCriterio.Aprovado, FvmAtendimento = ResultadoCriterio.Aprovado };
        parcial.AplicarNotas(0.4m, 0.4m, 0.2m, 6m);
        Assert.Null(parcial.Idf); // só com as 3 notas
        Assert.True(parcial.FvmAprovada);
        Assert.Equal(10m, parcial.NotaEntrega); // regra antiga: % de critérios aprovados

        Assert.Throws<Sigeo.Domain.Aprovacoes.RegraDeNegocioException>(() =>
            new Sigeo.Domain.Suprimentos.Recebimento { NotaPrazo = 11 }.AplicarNotas(0.4m, 0.4m, 0.2m, 6m));
    }

    [Fact]
    public void Periodo_trimestral_e_classificacao()
    {
        Assert.Equal("2026-T4", Sigeo.Domain.Suprimentos.AvaliacaoSemestral.PeriodoDe(new DateOnly(2026, 10, 14), 3));
        Assert.Equal("2026-S2", Sigeo.Domain.Suprimentos.AvaliacaoSemestral.PeriodoDe(new DateOnly(2026, 10, 14)));
        Assert.Equal((new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)), Sigeo.Domain.Suprimentos.AvaliacaoSemestral.Intervalo("2026-T2"));
        Assert.Equal((new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31)), Sigeo.Domain.Suprimentos.AvaliacaoSemestral.Intervalo("2026-S2"));
        Assert.Equal("Preferencial", Sigeo.Domain.Suprimentos.AvaliacaoSemestral.ClassificarIdf(8m, 8m, 6m));
        Assert.Equal("Sob acompanhamento", Sigeo.Domain.Suprimentos.AvaliacaoSemestral.ClassificarIdf(7.9m, 8m, 6m));
        Assert.Equal("Desqualificado", Sigeo.Domain.Suprimentos.AvaliacaoSemestral.ClassificarIdf(5.99m, 8m, 6m));
        Assert.Null(Sigeo.Domain.Suprimentos.AvaliacaoSemestral.ClassificarIdf(null, 8m, 6m));
    }
}
