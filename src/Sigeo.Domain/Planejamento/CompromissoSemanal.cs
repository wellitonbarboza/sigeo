using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>Compromisso da semana: uma atividade do cronograma (ou tarefa avulsa) com meta, responsável e apontamento.</summary>
public class CompromissoSemanal : Entidade
{
    public Guid PlanoId { get; set; }
    public int Ordem { get; set; }
    public string? AtividadeChave { get; set; }
    public string Descricao { get; set; } = string.Empty;
    /// <summary>Meta da semana em texto livre (ex.: "pav. 3 e 4", "60 m²").</summary>
    public string? Meta { get; set; }
    public string? Responsavel { get; set; }
    /// <summary>Feito sem estar no plano do início da semana — não entra no PPC.</summary>
    public bool ForaDaMeta { get; set; }
    public SituacaoCompromisso Situacao { get; set; } = SituacaoCompromisso.Pendente;
    public CausaNaoCumprimento? Causa { get; set; }
    public string? ObservacaoCausa { get; set; }

    public void Apontar(SituacaoCompromisso s, CausaNaoCumprimento? causa, string? obs)
    {
        if (s == SituacaoCompromisso.NaoConcluido && causa is null) throw new RegraDeNegocioException("Informe a causa do não cumprimento.");
        Situacao = s;
        Causa = s == SituacaoCompromisso.NaoConcluido ? causa : null;
        ObservacaoCausa = s == SituacaoCompromisso.NaoConcluido ? obs?.Trim() : null;
    }
}
