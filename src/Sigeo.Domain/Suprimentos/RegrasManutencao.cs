using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Regras de liberação do equipamento e indicadores de manutenção (funções puras).</summary>
public static class RegrasManutencao
{
    /// <summary>Motivos que bloqueiam o uso (vazio = liberado).</summary>
    public static List<string> Bloqueios(Equipamento e, IEnumerable<PlanoManutencao> planos, IEnumerable<OrdemServico> ordens, DateOnly hoje)
    {
        var b = new List<string>();
        if (e.Situacao is SituacaoEquipamento.Baixado or SituacaoEquipamento.Devolvido) b.Add(e.Situacao == SituacaoEquipamento.Baixado ? "baixado" : "devolvido à locadora");
        if (e.Situacao == SituacaoEquipamento.EmManutencao) b.Add("em manutenção");
        foreach (var d in e.Documentos.Where(d => d.Bloqueia && d.Vencimento is { } v && v < hoje)) b.Add($"{d.Nome} vencido em {d.Vencimento:dd/MM/yyyy}");
        foreach (var p in planos.Where(p => p.EquipamentoId == e.Id && p.Vencido(hoje, e.Horimetro + e.Hodometro)))
            b.Add($"preventiva vencida: {p.Nome}");
        foreach (var o in ordens.Where(o => o.EquipamentoId == e.Id && o.EmAberto && o.Tipo == TipoOrdemServico.Corretiva && (e.Criticidade == Criticidade.A || o.ParadoDesde is not null)))
            b.Add($"corretiva aberta (OS {o.Numero})");
        return b;
    }

    /// <summary>Disponibilidade no período = 1 − horas paradas / horas do período.</summary>
    public static decimal Disponibilidade(IEnumerable<OrdemServico> ordens, DateTimeOffset inicio, DateTimeOffset fim, int equipamentos)
    {
        if (equipamentos <= 0 || fim <= inicio) return 1;
        var total = (fim - inicio).TotalHours * equipamentos;
        var parado = ordens.Where(o => o.ParadoDesde is not null).Sum(o =>
        {
            var a = o.ParadoDesde!.Value < inicio ? inicio : o.ParadoDesde.Value;
            var b = (o.ParadoAte ?? fim) > fim ? fim : (o.ParadoAte ?? fim);
            return b > a ? (b - a).TotalHours : 0;
        });
        return Math.Round((decimal)Math.Max(0, 1 - parado / total), 4);
    }

    /// <summary>MTTR (horas para reparar) e MTBF (horas entre falhas) das corretivas no período.</summary>
    public static (decimal Mttr, decimal Mtbf) Indicadores(IEnumerable<OrdemServico> corretivas, double horasOperacao)
    {
        var lista = corretivas.Where(o => o.Tipo == TipoOrdemServico.Corretiva && o.ParadoDesde is not null && o.ParadoAte is not null).ToList();
        if (lista.Count == 0) return (0, (decimal)Math.Round(horasOperacao, 1));
        var mttr = lista.Average(o => (o.ParadoAte!.Value - o.ParadoDesde!.Value).TotalHours);
        return ((decimal)Math.Round(mttr, 1), (decimal)Math.Round(horasOperacao / lista.Count, 1));
    }
}
