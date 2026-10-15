using ClosedXML.Excel;
using Groundkey.Domain.Cadastros;
using Groundkey.Infrastructure.Importacao;

namespace Groundkey.Tests.Integracao;

/// <summary>Planilha de fornecedores: com CPF/CNPJ casa pelo documento; sem documento casa pelo nome e entra bloqueado. Dados fictícios.</summary>
[Collection(ColecaoBanco.Nome)]
public sealed class ImportacaoFornecedoresPlanilhaTests(BancoFixture f)
{
    [FatoComBanco]
    public async Task Sem_documento_casa_pelo_nome_e_nao_duplica()
    {
        var suf = Dados.Sufixo();
        var cnpj = Dados.Cnpj();
        // Fornecedor já criado sem documento (ex.: pela importação do histórico da obra).
        await using (var db = f.Fabrica.Criar())
        {
            db.Fornecedores.Add(new Fornecedor { CpfCnpj = "", RazaoSocial = $"Deposito Ficticio {suf} Ltda" });
            await db.SaveChangesAsync();
        }
        byte[] arquivo;
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Importar");
            string[] cab = ["CPF / CNPJ", "NOME", "CIDADE", "UF", "E-MAILS", "ORIGEM"];
            for (var c = 0; c < cab.Length; c++) ws.Cell(1, c + 1).Value = cab[c];
            ws.Cell(2, 1).Value = cnpj; ws.Cell(2, 2).Value = $"Ferragens Ficticia {suf} Ltda"; ws.Cell(2, 3).Value = "Cascavel"; ws.Cell(2, 4).Value = "PR"; ws.Cell(2, 6).Value = "ERP";
            ws.Cell(3, 2).Value = $"DEPÓSITO FICTICIO {suf} - ME"; ws.Cell(3, 5).Value = "contato@exemplo.com"; ws.Cell(3, 6).Value = "ERP";
            ws.Cell(4, 2).Value = $"Pedreiro Ficticio {suf}"; ws.Cell(4, 6).Value = "ERP";
            ws.Cell(5, 2).Value = $"PEDREIRO FICTICIO {suf}"; // repetido sem documento
            ws.Cell(6, 1).Value = "11.111.111/1111-11"; ws.Cell(6, 2).Value = $"Invalido {suf}";
            using var ms = new MemoryStream(); wb.SaveAs(ms); arquivo = ms.ToArray();
        }
        var imp = new ImportadorFornecedoresExpertData(f.Fabrica);
        var previa = await imp.PreverAsync(new MemoryStream(arquivo));
        Assert.Equal(["Inserir", "Atualizar", "Inserir", "Erro", "Erro"], previa.Linhas.Select(l => l.Acao).ToArray());
        Assert.Contains(previa.Linhas[1].Erros, e => e.Contains("bloqueado"));
        Assert.Contains("Duplicado", previa.Linhas[3].Erros[0]);

        var (ins, upd) = await imp.ImportarAsync(new MemoryStream(arquivo));
        Assert.Equal((2, 1), (ins, upd));
        await using var dbl = f.Fabrica.CriarLeitura();
        var dep = await dbl.Fornecedores.SingleAsync(x => x.RazaoSocial.Contains($"Ficticio {suf}") && x.RazaoSocial.StartsWith("D"));
        Assert.Equal("contato@exemplo.com", dep.Emails);
        Assert.Equal("", dep.CpfCnpj);
        var ped = await dbl.Fornecedores.SingleAsync(x => x.RazaoSocial == $"Pedreiro Ficticio {suf}");
        Assert.Equal(("", "ERP"), (ped.CpfCnpj, ped.OrigemSistema));
        Assert.Equal("FERRAGENS FICTICIA " + suf.ToUpperInvariant(), ImportadorFornecedoresExpertData.ChaveNome($"Ferragens Fictícia {suf} Ltda."));
        Assert.Equal("JOAO DA SILVA", ImportadorFornecedoresExpertData.ChaveNome("12.345.678 João da Silva"));
    }
}
