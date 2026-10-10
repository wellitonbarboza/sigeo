using QualyHub.Domain.Common;

namespace QualyHub.Domain.Planejamento;

/// <summary>
/// Medição semanal: percentual executado acumulado de um serviço (atividade do cronograma) num local.
/// Uma por obra × atividade × semana — medir de novo na mesma semana sobrepõe (a anterior fica no histórico temporal).
/// Quem mede é a Engenharia de obra; 100% é aceito com FVS aberta/não concluída, guardando o aviso.
/// </summary>
public class MedicaoSemanal : EntidadeDaObra, IHistoricoTemporal
{
    /// <summary>Chave estável da atividade (a mesma do gerador de redes), vale para todas as versões do cronograma.</summary>
    public string AtividadeChave { get; set; } = string.Empty;
    public Guid LocalId { get; set; }
    public Guid? PacoteId { get; set; }
    public Guid? ComposicaoId { get; set; }
    public string Servico { get; set; } = string.Empty;
    /// <summary>Segunda-feira da semana medida.</summary>
    public DateOnly Semana { get; set; }
    /// <summary>Data considerada como realização da medição (escolhida por quem mede).</summary>
    public DateOnly DataMedicao { get; set; }
    public decimal Percentual { get; set; }
    public decimal? PercentualAnterior { get; set; }
    /// <summary>Medido 100% com FVS do serviço aberta ou não concluída no local.</summary>
    public bool FvsPendente { get; set; }
    public string? Observacao { get; set; }
    public string MedidoPor { get; set; } = string.Empty;

    public static DateOnly SegundaDe(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}

/// <summary>Clima de um período do dia no diário de obra.</summary>
public enum ClimaDia { Bom = 0, Nublado = 1, ChuvaFraca = 2, ChuvaForte = 3 }

/// <summary>Condição do canteiro no dia.</summary>
public enum CondicaoDia { Praticavel = 0, ParcialmentePraticavel = 1, Impraticavel = 2 }

/// <summary>Se o dia foi trabalhado (base dos dias trabalhados × úteis e dos dias perdidos por chuva).</summary>
public enum DiaTrabalhado { Sim = 0, NaoChuva = 1, NaoFeriadoParalisacao = 2 }

public enum StatusDiario { Rascunho = 0, Assinado = 1 }

/// <summary>
/// Diário de obra (RDO): um por obra e dia. Preenche e assina o engenheiro(a) da obra; depois de assinado não muda.
/// Efetivo próprio por função (do controle de mão de obra) e de terceiros por nome do empreiteiro, ambos com quantidade.
/// Atividades em linhas, com local opcional e fotos (anexos da atividade).
/// </summary>
public class DiarioObra : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public DateOnly Data { get; set; }
    public ClimaDia ClimaManha { get; set; }
    public ClimaDia ClimaTarde { get; set; }
    public CondicaoDia Condicao { get; set; }
    public DiaTrabalhado Trabalhado { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraTermino { get; set; }
    public string? ObservacaoClima { get; set; }
    public string? Ocorrencias { get; set; }
    public string? Visitas { get; set; }
    public string? Equipamentos { get; set; }
    public StatusDiario Status { get; set; } = StatusDiario.Rascunho;
    public Guid? AssinadoPorId { get; set; }
    public string? AssinadoPorNome { get; set; }
    public DateTimeOffset? AssinadoEm { get; set; }
    public ICollection<DiarioEfetivo> Efetivo { get; set; } = [];
    public ICollection<DiarioAtividade> Atividades { get; set; } = [];

    public bool Editavel => Status == StatusDiario.Rascunho;
    public int TotalProprio => Efetivo.Where(e => !e.Terceiro).Sum(e => e.Quantidade);
    public int TotalTerceiros => Efetivo.Where(e => e.Terceiro).Sum(e => e.Quantidade);

    public IReadOnlyList<string> Validar(DateOnly hoje)
    {
        var e = new List<string>();
        if (Data == default) e.Add("Informe a data do diário.");
        else if (Data > hoje) e.Add("O diário não pode ser de data futura.");
        if (HoraInicio is { } i && HoraTermino is { } t && t <= i) e.Add("O término do expediente deve ser depois do início.");
        foreach (var x in Efetivo)
        {
            if (string.IsNullOrWhiteSpace(x.Nome)) e.Add(x.Terceiro ? "Efetivo de terceiros: informe o nome do terceiro." : "Efetivo próprio: escolha a função.");
            if (x.Quantidade is < 0 or > 2000) e.Add($"{x.Nome}: quantidade entre 0 e 2000.");
        }
        foreach (var dup in Efetivo.GroupBy(x => (x.Terceiro, Nome: x.Nome.Trim().ToUpperInvariant())).Where(g => g.Count() > 1 && g.Key.Nome.Length > 0))
            e.Add($"{dup.First().Nome} aparece mais de uma vez no efetivo {(dup.Key.Terceiro ? "de terceiros" : "próprio")}.");
        foreach (var a in Atividades.Where(a => string.IsNullOrWhiteSpace(a.Descricao))) e.Add($"Atividade {a.Ordem}: descreva o que foi feito.");
        return e;
    }

    public void Assinar(Guid usuarioId, string nome, DateTimeOffset agora)
    {
        if (!Editavel) throw new Aprovacoes.RegraDeNegocioException("O diário já está assinado.");
        Status = StatusDiario.Assinado; AssinadoPorId = usuarioId; AssinadoPorNome = nome; AssinadoEm = agora;
    }
}

/// <summary>Linha do efetivo: própria (Nome = função, FuncaoObraId) ou de terceiro (Nome = empreiteiro, FornecedorId opcional).</summary>
public class DiarioEfetivo : Entidade
{
    public Guid DiarioId { get; set; }
    public bool Terceiro { get; set; }
    public Guid? FuncaoObraId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

/// <summary>Atividade executada no dia: descrição, local opcional; as fotos são anexos ("DiarioAtividade", Id).</summary>
public class DiarioAtividade : Entidade
{
    public Guid DiarioId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Guid? LocalId { get; set; }
    public string? AtividadeChave { get; set; }
}
