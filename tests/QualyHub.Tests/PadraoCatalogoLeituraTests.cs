using ClosedXML.Excel;
using QualyHub.Application.Servicos;
using QualyHub.Domain.Common;
using QualyHub.Infrastructure.Importacao;
using Xunit;

namespace QualyHub.Tests;

/// <summary>Leitura da planilha do padrão do catálogo (abas Grupos, Reclassificar, Produtos).</summary>
public class PadraoCatalogoLeituraTests
{
    [Fact]
    public void Le_as_tres_abas_e_valida()
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Leia-me").Cell(1, 1).Value = "instruções";
        var g = wb.AddWorksheet("Grupos");
        string[] cg = ["Grupo", "Sigla", "Tipo de custo", "Fase padrão", "Material controlado"];
        for (var c = 0; c < cg.Length; c++) g.Cell(1, c + 1).Value = cg[c];
        g.Cell(2, 1).Value = "Fios e Cabos"; g.Cell(2, 2).Value = "fce"; g.Cell(2, 3).Value = "Material"; g.Cell(2, 4).Value = "Instalações elétricas"; g.Cell(2, 5).Value = "13";
        g.Cell(3, 1).Value = "Projetos"; g.Cell(3, 3).Value = "Serviço";
        var r = wb.AddWorksheet("Reclassificar");
        string[] cr = ["Grupo atual", "Subgrupo atual", "Nome contém", "Grupo novo", "Subgrupo novo", "Motivo"];
        for (var c = 0; c < cr.Length; c++) r.Cell(1, c + 1).Value = cr[c];
        r.Cell(2, 1).Value = "Material Elétrico"; r.Cell(2, 2).Value = "Cabos e Fios"; r.Cell(2, 4).Value = "Fios e Cabos"; r.Cell(2, 5).Value = "Cabos e Fios";
        var p = wb.AddWorksheet("Produtos");
        string[] cp = ["Nome", "Unidade", "Grupo", "Subgrupo", "Tipo de custo", "Código ERP", "Preço referência", "Categoria no ERP (info)"];
        for (var c = 0; c < cp.Length; c++) p.Cell(1, c + 1).Value = cp[c];
        p.Cell(2, 1).Value = "Cabo 2,5mm"; p.Cell(2, 2).Value = "M"; p.Cell(2, 3).Value = "Fios e Cabos"; p.Cell(2, 4).Value = "Cabos e Fios"; p.Cell(2, 6).Value = "123"; p.Cell(2, 7).Value = 3.5;
        using var ms = new MemoryStream(); wb.SaveAs(ms); ms.Position = 0;

        var padrao = LeitorPadraoCatalogo.Ler(ms);
        Assert.Equal(2, padrao.Grupos.Count);
        Assert.Equal((TipoCusto?)TipoCusto.Material, padrao.Grupos[0].TipoCusto);
        Assert.Equal((TipoCusto?)TipoCusto.Servico, padrao.Grupos[1].TipoCusto);
        Assert.Equal("13", padrao.Grupos[0].MaterialCodigo);
        Assert.Null(padrao.Regras[0].Contem);
        Assert.Equal(3.5m, padrao.Produtos[0].Preco);
        Assert.Null(padrao.Produtos[0].TipoCusto);
        Assert.Empty(PadraoCatalogoServico.Validar(padrao)); // sigla "fce" vira FCE
        Assert.Equal(PadraoCatalogoServico.ChaveProduto("Cabo  2,5MM", "m"), PadraoCatalogoServico.ChaveProduto("cabo 2,5mm", "M"));
        Assert.Contains(PadraoCatalogoServico.Validar(padrao with { Grupos = [.. padrao.Grupos, new PadraoGrupo(9, "Outro", "X1Y2", null, null, "13")] }),
            e => e.Contains("sigla") || e.Contains("mais de um grupo"));
    }
}
