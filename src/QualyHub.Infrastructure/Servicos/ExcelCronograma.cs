using ClosedXML.Excel;
using QualyHub.Domain.Planejamento;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Cronograma em Excel: atividades com EAP em níveis (agrupamento), predecessoras TI/II/TT/IT, folga, base e desembolso mensal.</summary>
public static class ExcelCronograma
{
    public static byte[] Gerar(string obra, VersaoCronograma v, Calendario cal)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cronograma");
        ws.Cell(1, 1).Value = $"Cronograma · {obra} · versão {v.Codigo}";
        ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = $"Início {v.InicioProjeto:dd/MM/yyyy} · término {v.TerminoPrevisto:dd/MM/yyyy} · status {v.DataStatus:dd/MM/yyyy} · dias úteis (seg–sex, feriados)";
        string[] cab = ["EAP", "Atividade", "Duração (d.ú.)", "Início", "Término", "Predecessoras", "Avanço %", "Folga (d.ú.)", "Crítica", "Custo orçado", "Início base", "Término base", "Δ término (d.ú.)"];
        for (var i = 0; i < cab.Length; i++) ws.Cell(4, i + 1).Value = cab[i];
        var h = ws.Range(4, 1, 4, cab.Length);
        h.Style.Font.Bold = true; h.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E1FB");
        var porId = v.Atividades.ToDictionary(a => a.Id);
        var vinc = v.Vinculos.ToList();
        var filhos = v.Atividades.ToLookup(a => a.PaiId);
        var linha = 5;
        void Escrever(Guid? pai, int nivel)
        {
            foreach (var a in filhos[pai].OrderBy(a => a.Ordem))
            {
                var r = ws.Row(linha);
                ws.Cell(linha, 1).Value = a.Eap;
                ws.Cell(linha, 2).Value = new string(' ', (nivel - 1) * 3) + a.Nome;
                ws.Cell(linha, 3).Value = a.Duracao;
                ws.Cell(linha, 4).Value = a.Inicio.ToDateTime(TimeOnly.MinValue);
                ws.Cell(linha, 5).Value = a.Termino.ToDateTime(TimeOnly.MinValue);
                ws.Cell(linha, 6).Value = a.Resumo ? "" : MotorCronograma.FormatarPredecessoras(a.Id, vinc, porId);
                ws.Cell(linha, 7).Value = a.Avanco;
                if (a.Folga is { } f) ws.Cell(linha, 8).Value = f;
                ws.Cell(linha, 9).Value = a.Critica ? "sim" : "";
                ws.Cell(linha, 10).Value = a.Custo;
                if (a.InicioBase is { } bi) ws.Cell(linha, 11).Value = bi.ToDateTime(TimeOnly.MinValue);
                if (a.TerminoBase is { } bf) { ws.Cell(linha, 12).Value = bf.ToDateTime(TimeOnly.MinValue); ws.Cell(linha, 13).Value = cal.DiasUteisEntre(bf, a.Termino); }
                if (a.Resumo) r.Style.Font.Bold = true;
                if (a.Critica && !a.Resumo) ws.Cell(linha, 2).Style.Font.FontColor = XLColor.FromHtml("#B42318");
                if (nivel > 1) r.OutlineLevel = Math.Min(7, nivel - 1);
                linha++;
                Escrever(a.Id, nivel + 1);
            }
        }
        Escrever(null, 1);
        ws.Columns(4, 5).Style.DateFormat.Format = "dd/mm/yyyy";
        ws.Columns(11, 12).Style.DateFormat.Format = "dd/mm/yyyy";
        ws.Column(10).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(1).Width = 10; ws.Column(2).Width = 55; ws.Columns(3, 13).Width = 13; ws.Column(6).Width = 24;
        ws.SheetView.FreezeRows(4);
        if (linha > 5) ws.Range(4, 1, linha - 1, cab.Length).SetAutoFilter();

        var wd = wb.Worksheets.Add("Desembolso");
        string[] cd = ["Mês", "Base", v.Codigo, "Diferença", "Acumulado base", $"Acumulado {v.Codigo}"];
        for (var i = 0; i < cd.Length; i++) wd.Cell(1, i + 1).Value = cd[i];
        wd.Range(1, 1, 1, cd.Length).Style.Font.Bold = true;
        var atual = Desembolso.PorMes(v.Atividades, cal); var baseM = Desembolso.PorMes(v.Atividades, cal, usarBase: true);
        decimal ab = 0, aa = 0; var l2 = 2;
        foreach (var m in atual.Keys.Union(baseM.Keys).Distinct().Order())
        {
            var b = baseM.GetValueOrDefault(m); var a = atual.GetValueOrDefault(m); ab += b; aa += a;
            wd.Cell(l2, 1).Value = m.ToDateTime(TimeOnly.MinValue); wd.Cell(l2, 2).Value = b; wd.Cell(l2, 3).Value = a; wd.Cell(l2, 4).Value = a - b;
            wd.Cell(l2, 5).Value = ab; wd.Cell(l2, 6).Value = aa; l2++;
        }
        wd.Column(1).Style.DateFormat.Format = "mm/yyyy";
        wd.Columns(2, 6).Style.NumberFormat.Format = "#,##0.00";
        wd.Columns(1, 6).Width = 18;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
