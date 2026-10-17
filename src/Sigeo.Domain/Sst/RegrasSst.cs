using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public static class RegrasSst
{
    /// <summary>
    /// Situação da pessoa pelas exigências da função: O vencida/ausente que bloqueia → Bloqueado; C vencida → restringe a atividade;
    /// só alerta → Pendente; tudo em dia → Apto.
    /// </summary>
    public static SituacaoCalculada Calcular(Colaborador c, IEnumerable<FuncaoExigencia> matriz, IReadOnlyDictionary<Guid, Exigencia> exigencias,
        IEnumerable<DocumentoColaborador> docs, IEnumerable<(Guid TreinamentoId, DateOnly? ValidoAte)> treinamentos, bool temEpi, DateOnly hoje)
    {
        if (c.DataSaida is { } s && s <= hoje) return new(SituacaoColaborador.DesligadoDaObra, AtividadeRisco.Nenhuma, []);
        var detalhe = new List<(string, string, DateOnly?, bool)>();
        var bloqueado = false; var pendente = false; var restricoes = AtividadeRisco.Nenhuma;
        foreach (var fe in matriz.Where(m => m.FuncaoObraId == c.FuncaoObraId))
        {
            if (!exigencias.TryGetValue(fe.ExigenciaId, out var ex)) continue;
            DateOnly? validade = null; bool ok; string sit;
            switch (ex.Tipo)
            {
                case TipoExigencia.Treinamento:
                    var t = treinamentos.Where(x => x.TreinamentoId == ex.TreinamentoId).OrderByDescending(x => x.ValidoAte).FirstOrDefault();
                    validade = t.ValidoAte == DateOnly.MaxValue ? null : t.ValidoAte;
                    ok = t.ValidoAte is { } vt && vt >= hoje;
                    sit = t.TreinamentoId == Guid.Empty ? "Não realizado" : ok ? "Válido" : "Vencido";
                    break;
                case TipoExigencia.Epi:
                    ok = temEpi; sit = ok ? "Em dia" : "Sem entrega registrada";
                    break;
                default:
                    var d = docs.Where(x => x.ExigenciaId == ex.Id && x.Validacao == ValidacaoDoc.Validado).OrderByDescending(x => x.DataEmissao).FirstOrDefault();
                    validade = d?.Validade;
                    ok = d is not null && (d.Validade is null || d.Validade >= hoje) && (ex.Tipo != TipoExigencia.ExameAso || d.ConclusaoAso == ConclusaoAso.Apto);
                    sit = d is null ? (docs.Any(x => x.ExigenciaId == ex.Id && x.Validacao == ValidacaoDoc.Aguardando) ? "Em validação" : "Pendente")
                        : ex.Tipo == TipoExigencia.ExameAso && d.ConclusaoAso == ConclusaoAso.Inapto ? "Inapto"
                        : ok ? (ex.Tipo == TipoExigencia.ExameAso ? "Apto · válido" : "Válido") : "Vencido";
                    if (ex.Tipo == TipoExigencia.ExameAso && d is not null && ok)
                        restricoes |= AtividadeRiscoTodas & ~d.AptoAtividades & AtividadesExigidas(matriz, exigencias, c.FuncaoObraId);
                    break;
            }
            detalhe.Add((ex.Descricao, sit, validade, ok));
            if (ok) continue;
            if (fe.Obrigatoriedade == Obrigatoriedade.ConformeAtividade || ex.EfeitoVencimento == EfeitoVencimento.RestringeAtividade)
                restricoes |= ex.Atividades == AtividadeRisco.Nenhuma ? AtividadeRisco.Nenhuma : ex.Atividades;
            else if (ex.EfeitoVencimento == EfeitoVencimento.BloqueiaPessoa) bloqueado = true;
            else pendente = true;
        }
        var situacao = bloqueado ? SituacaoColaborador.Bloqueado
            : restricoes != AtividadeRisco.Nenhuma ? SituacaoColaborador.AptoComRestricao
            : pendente ? SituacaoColaborador.Pendente : SituacaoColaborador.Apto;
        return new(situacao, restricoes, detalhe);
    }

    private const AtividadeRisco AtividadeRiscoTodas = AtividadeRisco.Altura | AtividadeRisco.EspacoConfinado | AtividadeRisco.Eletrico | AtividadeRisco.OperacaoEquipamento;

    private static AtividadeRisco AtividadesExigidas(IEnumerable<FuncaoExigencia> matriz, IReadOnlyDictionary<Guid, Exigencia> ex, Guid funcao) =>
        matriz.Where(m => m.FuncaoObraId == funcao && ex.ContainsKey(m.ExigenciaId))
              .Aggregate(AtividadeRisco.Nenhuma, (a, m) => a | ex[m.ExigenciaId].Atividades);

    /// <summary>Executante só entra na PT se apto e sem restrição na atividade do tipo de PT.</summary>
    public static (bool Liberado, string? Motivo) PodeExecutar(Colaborador c, TipoPermissaoTrabalho tipo)
    {
        if (c.Situacao is SituacaoColaborador.Bloqueado or SituacaoColaborador.DesligadoDaObra) return (false, $"Colaborador {De(c.Situacao)}.");
        var risco = PermissaoTrabalho.Risco(tipo);
        if ((c.Restricoes & risco) != 0) return (false, "Exigência da atividade vencida ou ausente.");
        return (true, null);
    }

    private static string De(SituacaoColaborador s) => s switch { SituacaoColaborador.Bloqueado => "bloqueado", SituacaoColaborador.DesligadoDaObra => "desligado da obra", _ => s.ToString() };

    /// <summary>Taxa de frequência (NBR 14280): acidentes com afastamento × 1.000.000 / horas-homem.</summary>
    public static decimal? TaxaFrequencia(int acidentesComAfastamento, decimal horasHomem) =>
        horasHomem <= 0 ? null : Math.Round(acidentesComAfastamento * 1_000_000m / horasHomem, 2);
}
