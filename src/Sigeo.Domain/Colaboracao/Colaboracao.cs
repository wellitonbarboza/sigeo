using Sigeo.Domain.Common;
using Sigeo.Domain.Aprovacoes;

namespace Sigeo.Domain.Colaboracao;

/// <summary>
/// Nota (comentário) de qualquer registro — histórico com data/hora e usuário, nunca um campo único sobrescrito.
/// Imutável depois de gravada: correção = nova nota.
/// </summary>
public class Nota : EntidadeAuditavel
{
    /// <summary>Nome da entidade dona (ex.: "Requisicao", "Cotacao", "Rnc").</summary>
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public string Texto { get; set; } = string.Empty;

    public static Nota Criar(string entidade, Guid entidadeId, string texto)
    {
        var t = (texto ?? "").Trim();
        if (t.Length == 0) throw new RegraDeNegocioException("Escreva a nota.");
        if (t.Length > 4000) throw new RegraDeNegocioException("Nota com mais de 4.000 caracteres.");
        if (string.IsNullOrWhiteSpace(entidade) || entidadeId == Guid.Empty) throw new RegraDeNegocioException("Salve o registro antes de incluir notas.");
        return new Nota { Entidade = entidade, EntidadeId = entidadeId, Texto = t };
    }
}

/// <summary>
/// Tarefa interna com prazo (substitui e-mail solto): responsável, prioridade, prorrogação justificada
/// e confirmação de leitura. Pode nascer de uma RNC, ação corretiva, auditoria, plano de ação etc. (Origem).
/// </summary>
public class Tarefa : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    /// <summary>Lista configurável (Compras, Qualidade, SST, Obra, Documentos…).</summary>
    public string? Tipo { get; set; }
    public PrioridadeTarefa Prioridade { get; set; } = PrioridadeTarefa.Normal;
    public Guid? ObraId { get; set; }
    public Guid ResponsavelId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public DateOnly PrazoOriginal { get; set; }
    public DateOnly Prazo { get; set; }
    public StatusTarefa Status { get; set; } = StatusTarefa.Aberta;
    public string? Resolucao { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
    public string? ConcluidaPor { get; set; }
    /// <summary>Registro que originou a tarefa (ex.: "Rnc"), para navegar e fechar o ciclo.</summary>
    public string? OrigemEntidade { get; set; }
    public Guid? OrigemId { get; set; }
    public string? OrigemDescricao { get; set; }

    public ICollection<TarefaProrrogacao> Prorrogacoes { get; set; } = [];
    public ICollection<TarefaLeitura> Leituras { get; set; } = [];

    public bool Encerrada => Status is StatusTarefa.Concluida or StatusTarefa.Cancelada;
    public bool Atrasada(DateOnly hoje) => !Encerrada && Prazo < hoje;
    public int DiasRestantes(DateOnly hoje) => Prazo.DayNumber - hoje.DayNumber;

    public IReadOnlyList<string> Validar(DateOnly hoje)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Titulo)) e.Add("Informe o título.");
        if (ResponsavelId == Guid.Empty) e.Add("Informe o responsável.");
        if (Prazo == default) e.Add("Informe o prazo.");
        else if (CriadoEm == default && Prazo < hoje) e.Add("Prazo não pode ser no passado.");
        return e;
    }

    public void PrepararNova()
    {
        if (PrazoOriginal == default) PrazoOriginal = Prazo;
    }

    /// <summary>Prorrogação sempre para frente, com justificativa; o prazo original fica guardado.</summary>
    public TarefaProrrogacao Prorrogar(DateOnly novoPrazo, string justificativa, string usuario)
    {
        if (Encerrada) throw new RegraDeNegocioException("Tarefa encerrada não pode ser prorrogada.");
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length < 10) throw new RegraDeNegocioException("Justifique a prorrogação (mínimo 10 caracteres).");
        if (novoPrazo <= Prazo) throw new RegraDeNegocioException("O novo prazo deve ser posterior ao atual.");
        var p = new TarefaProrrogacao
        {
            TarefaId = Id, PrazoAnterior = Prazo, NovoPrazo = novoPrazo, Dias = novoPrazo.DayNumber - Prazo.DayNumber,
            Justificativa = justificativa.Trim(), Usuario = usuario
        };
        Prazo = novoPrazo;
        Prorrogacoes.Add(p);
        return p;
    }

    public void Iniciar() { if (Status == StatusTarefa.Aberta) Status = StatusTarefa.EmAndamento; }

    public void Concluir(string resolucao, string usuario, DateTimeOffset agora)
    {
        if (Encerrada) throw new RegraDeNegocioException("Tarefa já encerrada.");
        if (string.IsNullOrWhiteSpace(resolucao)) throw new RegraDeNegocioException("Descreva a resolução para concluir.");
        Status = StatusTarefa.Concluida; Resolucao = resolucao.Trim(); ConcluidaEm = agora; ConcluidaPor = usuario;
    }

    public void Cancelar(string motivo, string usuario, DateTimeOffset agora)
    {
        if (Encerrada) throw new RegraDeNegocioException("Tarefa já encerrada.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        Status = StatusTarefa.Cancelada; Resolucao = $"Cancelada: {motivo.Trim()}"; ConcluidaEm = agora; ConcluidaPor = usuario;
    }

    /// <summary>Confirmação de leitura: 1 registro por usuário (a primeira leitura vale).</summary>
    public TarefaLeitura? RegistrarLeitura(Guid usuarioId, string nome, DateTimeOffset agora)
    {
        if (Leituras.Any(l => l.UsuarioId == usuarioId)) return null;
        var l = new TarefaLeitura { TarefaId = Id, UsuarioId = usuarioId, Nome = nome, LidaEm = agora };
        Leituras.Add(l);
        return l;
    }
}

public class TarefaProrrogacao : Entidade
{
    public Guid TarefaId { get; set; }
    public Tarefa Tarefa { get; set; } = null!;
    public DateOnly PrazoAnterior { get; set; }
    public DateOnly NovoPrazo { get; set; }
    public int Dias { get; set; }
    public string Justificativa { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTimeOffset Em { get; set; } = DateTimeOffset.UtcNow;
}

public class TarefaLeitura : Entidade
{
    public Guid TarefaId { get; set; }
    public Tarefa Tarefa { get; set; } = null!;
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset LidaEm { get; set; }
}
