using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Domain.Planejamento;

/// <summary>Tipo de restrição (o que impede o pacote de começar) — médio prazo / lookahead.</summary>
public enum TipoRestricao { Projeto = 1, Material = 2, MaoDeObra = 3, Equipamento = 4, Contrato = 5, Financeiro = 6, Documentacao = 7, ServicoAnterior = 8, Outro = 9 }

public enum StatusRestricao { Aberta = 0, Removida = 1, Cancelada = 2 }

/// <summary>Situação do plano semanal: aberto (montando/apontando) ou fechado (PPC apurado, não muda mais).</summary>
public enum StatusPlanoSemanal { Aberto = 0, Fechado = 1 }

public enum SituacaoCompromisso { Pendente = 0, Concluido = 1, NaoConcluido = 2 }

/// <summary>Causa de não cumprimento do compromisso semanal (Last Planner).</summary>
public enum CausaNaoCumprimento
{
    Material = 1, MaoDeObra = 2, Projeto = 3, Equipamento = 4, Clima = 5, ServicoAnterior = 6,
    Fornecedor = 7, Planejamento = 8, Qualidade = 9, Seguranca = 10, Outra = 99
}

/// <summary>
/// Restrição que impede uma atividade do cronograma de começar (projeto, material, mão de obra, contrato…).
/// Ligada à atividade pela chave estável (rede|pacote|local) para continuar valendo entre reprogramações.
/// Atividade com restrição aberta não entra no plano semanal.
/// </summary>
public class Restricao : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public TipoRestricao Tipo { get; set; } = TipoRestricao.Material;
    public string Descricao { get; set; } = string.Empty;
    /// <summary>Chave estável da atividade (Chave do gerador; para atividade manual, "A:{id}").</summary>
    public string AtividadeChave { get; set; } = string.Empty;
    public string AtividadeNome { get; set; } = string.Empty;
    public DateOnly? InicioPrevisto { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    /// <summary>Data até a qual a restrição precisa estar removida (normalmente antes do início da atividade).</summary>
    public DateOnly DataLimite { get; set; }
    public StatusRestricao Status { get; set; } = StatusRestricao.Aberta;
    public string? Resolucao { get; set; }
    public DateTimeOffset? RemovidaEm { get; set; }
    public string? RemovidaPor { get; set; }
    /// <summary>Documento que gerou a restrição automaticamente (transferência de material, OS de equipamento): ao concluir, ela é removida sozinha.</summary>
    public Guid? OrigemId { get; set; }

    public bool Aberta => Status == StatusRestricao.Aberta;
    public bool Vencida(DateOnly hoje) => Aberta && DataLimite < hoje;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a restrição.");
        if (string.IsNullOrWhiteSpace(AtividadeChave)) e.Add("Escolha a atividade do cronograma.");
        if (string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável por remover a restrição.");
        if (DataLimite == default) e.Add("Informe a data-limite.");
        return e;
    }

    public void Remover(string comoFoiRemovida, string usuario, DateTimeOffset agora)
    {
        if (!Aberta) throw new RegraDeNegocioException("Restrição já encerrada.");
        if (string.IsNullOrWhiteSpace(comoFoiRemovida)) throw new RegraDeNegocioException("Diga como a restrição foi removida.");
        Status = StatusRestricao.Removida; Resolucao = comoFoiRemovida.Trim(); RemovidaEm = agora; RemovidaPor = usuario;
    }

    public void Cancelar(string motivo, string usuario, DateTimeOffset agora)
    {
        if (!Aberta) throw new RegraDeNegocioException("Restrição já encerrada.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        Status = StatusRestricao.Cancelada; Resolucao = $"Cancelada: {motivo.Trim()}"; RemovidaEm = agora; RemovidaPor = usuario;
    }
}

/// <summary>
/// Plano semanal (Last Planner / curto prazo): o que a obra se compromete a fazer na semana (segunda a domingo).
/// Fechado, apura o PPC = compromissos concluídos ÷ compromissos planejados. O que foi feito sem estar no plano
/// entra como "fora da meta" e não conta no PPC.
/// </summary>
public class PlanoSemanal : EntidadeDaObra, IHistoricoTemporal
{
    public DateOnly Semana { get; set; }
    public StatusPlanoSemanal Status { get; set; } = StatusPlanoSemanal.Aberto;
    public string? Observacao { get; set; }
    public DateTimeOffset? FechadoEm { get; set; }
    public string? FechadoPor { get; set; }
    /// <summary>PPC apurado no fechamento (0–100), guardado para a série histórica não mudar depois.</summary>
    public decimal? Ppc { get; set; }
    public ICollection<CompromissoSemanal> Compromissos { get; set; } = [];

    public DateOnly Fim => Semana.AddDays(6);
    public bool Editavel => Status == StatusPlanoSemanal.Aberto;

    /// <summary>Segunda-feira da semana da data.</summary>
    public static DateOnly SegundaDe(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static (int Planejados, int Concluidos, int ForaDaMeta, decimal? Ppc) Apurar(IEnumerable<CompromissoSemanal> itens)
    {
        var l = itens.ToList();
        var plan = l.Count(c => !c.ForaDaMeta);
        var ok = l.Count(c => !c.ForaDaMeta && c.Situacao == SituacaoCompromisso.Concluido);
        var fora = l.Count(c => c.ForaDaMeta && c.Situacao == SituacaoCompromisso.Concluido);
        return (plan, ok, fora, plan == 0 ? null : Math.Round(100m * ok / plan, 1));
    }

    /// <summary>Fecha a semana: todo compromisso planejado precisa estar apontado; não concluído precisa de causa.</summary>
    public void Fechar(string usuario, DateTimeOffset agora)
    {
        if (!Editavel) throw new RegraDeNegocioException("Semana já fechada.");
        var erros = new List<string>();
        foreach (var c in Compromissos)
        {
            if (c.Situacao == SituacaoCompromisso.Pendente) erros.Add($"Aponte \"{c.Descricao}\" (concluído ou não).");
            else if (c.Situacao == SituacaoCompromisso.NaoConcluido && c.Causa is null) erros.Add($"Informe a causa de \"{c.Descricao}\" não ter sido concluído.");
        }
        if (Compromissos.Count == 0) erros.Add("A semana não tem compromissos.");
        if (erros.Count > 0) throw new RegraDeNegocioException(string.Join(" ", erros.Take(5)) + (erros.Count > 5 ? $" (+{erros.Count - 5})" : ""));
        Ppc = Apurar(Compromissos).Ppc;
        Status = StatusPlanoSemanal.Fechado; FechadoEm = agora; FechadoPor = usuario;
    }
}

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
