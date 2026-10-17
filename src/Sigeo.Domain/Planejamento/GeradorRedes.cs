namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Gera o cronograma a partir das redes (função pura):
/// rede (agrupador) › pacote de serviço (agrupador) › uma atividade por local incluído.
/// Vínculos: fluxo das equipes (o local k espera o local k − nº de equipes do mesmo pacote), sequência dos pacotes
/// no mesmo local (+ pulmão) e vínculos entre redes (por pavimento ou após o fim da rede de origem).
/// O custo de cada atividade vem do orçamento (serviço × local, somando a subárvore do local).
/// </summary>
public static class GeradorRedes
{
    public sealed record LocalDef(Guid Id, string Nome, int Pavimento);
    public sealed record PacoteDef(Guid Id, string Nome, Guid? ComposicaoId, int Duracao, int Equipes, IReadOnlySet<Guid> Locais);
    public sealed record RedeDef(Guid Id, string Nome, TipoRede Tipo, int Pulmao, IReadOnlyList<LocalDef> Locais, IReadOnlyList<PacoteDef> Pacotes);
    public sealed record VinculoRedeDef(Guid PacoteOrigemId, Guid PacoteDestinoId, ModoVinculoRede Modo, int Deslocamento, int Lag);
    public sealed record Geracao(List<AtividadeCronograma> Atividades, List<VinculoAtividade> Vinculos);

    public static string ChaveRede(Guid rede) => $"R:{rede:N}";
    public static string ChavePacote(Guid pacote) => $"P:{pacote:N}";
    public static string ChaveLocal(Guid pacote, Guid local) => $"L:{pacote:N}:{local:N}";

    public static Geracao Gerar(Guid versaoId, IReadOnlyList<RedeDef> redes, IReadOnlyList<VinculoRedeDef> vinculosRede,
        Func<Guid, Guid, decimal> custo, int ordemInicial = 0)
    {
        var ativ = new List<AtividadeCronograma>();
        var vinc = new List<VinculoAtividade>();
        // pacote → folhas na ordem dos locais da rede
        var folhasDoPacote = new Dictionary<Guid, List<(LocalDef Local, AtividadeCronograma A)>>();
        var ordemRede = ordemInicial;
        void Ligar(AtividadeCronograma p, AtividadeCronograma s, int lag = 0)
        {
            if (p.Id == s.Id || vinc.Any(v => v.PredecessoraId == p.Id && v.SucessoraId == s.Id)) return;
            vinc.Add(new VinculoAtividade { VersaoId = versaoId, PredecessoraId = p.Id, SucessoraId = s.Id, Tipo = TipoVinculo.TerminoInicio, Lag = lag });
        }

        foreach (var rede in redes)
        {
            var r = new AtividadeCronograma { VersaoId = versaoId, Nome = rede.Nome, Resumo = true, Ordem = ++ordemRede, RedeId = rede.Id, Chave = ChaveRede(rede.Id) };
            ativ.Add(r);
            var ultimoNoLocal = new Dictionary<Guid, AtividadeCronograma>();
            var ordemPacote = 0;
            foreach (var pac in rede.Pacotes)
            {
                var locais = rede.Locais.Where(l => pac.Locais.Contains(l.Id)).ToList();
                if (locais.Count == 0) continue;
                var p = new AtividadeCronograma { VersaoId = versaoId, PaiId = r.Id, Nome = pac.Nome, Resumo = true, Ordem = ++ordemPacote, RedeId = rede.Id, PacoteId = pac.Id, ComposicaoId = pac.ComposicaoId, Chave = ChavePacote(pac.Id) };
                ativ.Add(p);
                var lista = new List<(LocalDef, AtividadeCronograma)>();
                var eq = Math.Max(1, pac.Equipes);
                for (var k = 0; k < locais.Count; k++)
                {
                    var l = locais[k];
                    var f = new AtividadeCronograma
                    {
                        VersaoId = versaoId, PaiId = p.Id, Nome = $"{pac.Nome} · {l.Nome}", Ordem = k + 1, Duracao = Math.Max(0, pac.Duracao),
                        RedeId = rede.Id, PacoteId = pac.Id, ComposicaoId = pac.ComposicaoId, LocalId = l.Id, Chave = ChaveLocal(pac.Id, l.Id),
                        Custo = pac.ComposicaoId is { } c ? Math.Round(custo(c, l.Id), 2) : 0
                    };
                    ativ.Add(f);
                    if (k - eq >= 0) Ligar(lista[k - eq].Item2, f);                                   // fluxo das equipes
                    if (ultimoNoLocal.TryGetValue(l.Id, out var antes)) Ligar(antes, f, rede.Pulmao);   // sequência no local
                    ultimoNoLocal[l.Id] = f;
                    lista.Add((l, f));
                }
                folhasDoPacote[pac.Id] = lista;
            }
        }

        foreach (var vr in vinculosRede)
        {
            if (!folhasDoPacote.TryGetValue(vr.PacoteOrigemId, out var origem) || !folhasDoPacote.TryGetValue(vr.PacoteDestinoId, out var destino)) continue;
            if (origem.Count == 0 || destino.Count == 0) continue;
            if (vr.Modo == ModoVinculoRede.AposFimDaRede)
            {
                Ligar(origem[^1].A, destino[0].A, vr.Lag);
                continue;
            }
            var origemPorPav = origem.OrderBy(o => o.Local.Pavimento).ToList();
            foreach (var pav in destino.Select(d => d.Local.Pavimento).Distinct())
            {
                var alvo = pav + vr.Deslocamento;
                var o = origemPorPav.Where(x => x.Local.Pavimento >= alvo).Select(x => x.A).FirstOrDefault() ?? origemPorPav[^1].A;
                // basta segurar o primeiro local do pavimento no destino; os demais seguem pelo fluxo das equipes
                foreach (var d in destino.Where(d => d.Local.Pavimento == pav).Take(1)) Ligar(o, d.A, vr.Lag);
            }
        }
        return new Geracao(ativ, vinc);
    }

    /// <summary>
    /// Junta a geração nova com a versão atual: atividades com a mesma chave mantêm avanço, linha de base, início mínimo
    /// e a folga dos vínculos que o usuário ajustou (arrastar). Atividades manuais (sem chave) ficam, com seus vínculos.
    /// </summary>
    public static Geracao Mesclar(IReadOnlyCollection<AtividadeCronograma> atuais, IReadOnlyCollection<VinculoAtividade> vinculosAtuais, Geracao nova)
    {
        var porChave = atuais.Where(a => a.Chave != null).GroupBy(a => a.Chave!).ToDictionary(g => g.Key, g => g.First());
        var idNovoPorAntigo = new Dictionary<Guid, Guid>();
        foreach (var a in nova.Atividades)
        {
            if (a.Chave is null || !porChave.TryGetValue(a.Chave, out var velha)) continue;
            idNovoPorAntigo[velha.Id] = a.Id;
            a.Avanco = velha.Avanco; a.InicioBase = velha.InicioBase; a.TerminoBase = velha.TerminoBase; a.InicioMinimo = velha.InicioMinimo;
            if (!velha.Resumo && velha.Duracao != a.Duracao && velha.Avanco > 0) a.Duracao = velha.Duracao; // em execução: mantém a duração já ajustada
        }
        var chaveDe = atuais.ToDictionary(a => a.Id, a => a.Chave);
        var lagAjustado = vinculosAtuais.Where(v => chaveDe.GetValueOrDefault(v.PredecessoraId) != null && chaveDe.GetValueOrDefault(v.SucessoraId) != null)
            .GroupBy(v => (chaveDe[v.PredecessoraId]!, chaveDe[v.SucessoraId]!)).ToDictionary(g => g.Key, g => g.First());
        var chaveNova = nova.Atividades.ToDictionary(a => a.Id, a => a.Chave);
        foreach (var v in nova.Vinculos)
            if (chaveNova[v.PredecessoraId] is { } cp && chaveNova[v.SucessoraId] is { } cs && lagAjustado.TryGetValue((cp, cs), out var antigo))
            { v.Lag = antigo.Lag; v.Tipo = antigo.Tipo; }

        // manuais: atividades sem chave e vínculos que tocam nelas
        var manuais = atuais.Where(a => a.Chave is null).ToList();
        var maxOrdemRaiz = nova.Atividades.Where(a => a.PaiId == null).Select(a => a.Ordem).DefaultIfEmpty(0).Max();
        foreach (var m in manuais)
        {
            if (m.PaiId is { } pai) m.PaiId = idNovoPorAntigo.TryGetValue(pai, out var np) ? np : manuais.Any(x => x.Id == pai) ? pai : null;
            if (m.PaiId is null) m.Ordem = ++maxOrdemRaiz;
        }
        var idsFinais = nova.Atividades.Select(a => a.Id).Concat(manuais.Select(m => m.Id)).ToHashSet();
        Guid Mapear(Guid id) => idNovoPorAntigo.TryGetValue(id, out var n) ? n : id;
        var manuaisIds = manuais.Select(m => m.Id).ToHashSet();
        var vincManuais = vinculosAtuais.Where(v => manuaisIds.Contains(v.PredecessoraId) || manuaisIds.Contains(v.SucessoraId))
            .Select(v => new VinculoAtividade { VersaoId = v.VersaoId, PredecessoraId = Mapear(v.PredecessoraId), SucessoraId = Mapear(v.SucessoraId), Tipo = v.Tipo, Lag = v.Lag })
            .Where(v => idsFinais.Contains(v.PredecessoraId) && idsFinais.Contains(v.SucessoraId));
        return new Geracao([.. nova.Atividades, .. manuais], [.. nova.Vinculos, .. vincManuais]);
    }
}
