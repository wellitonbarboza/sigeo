using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Atividade do cronograma. Resumo = agrupador (rede, pacote ou etapa) com datas/custo somados dos filhos.
/// Folha gerada por rede tem Chave estável (rede|pacote|local) para preservar avanço, base e folgas ao regerar.
/// </summary>
public class AtividadeCronograma : Entidade
{
    public Guid VersaoId { get; set; }
    public Guid? PaiId { get; set; }
    public int Ordem { get; set; }
    public string Eap { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Duração em dias úteis (0 = marco).</summary>
    public int Duracao { get; set; } = 1;
    public DateOnly Inicio { get; set; }
    public DateOnly Termino { get; set; }
    public DateOnly? InicioBase { get; set; }
    public DateOnly? TerminoBase { get; set; }
    /// <summary>Só para atividade sem predecessoras: não começar antes (arrastar uma atividade com predecessora vira folga no vínculo).</summary>
    public DateOnly? InicioMinimo { get; set; }
    public decimal Avanco { get; set; }
    public decimal Custo { get; set; }
    public Guid? ComposicaoId { get; set; }
    public Guid? LocalId { get; set; }
    public Guid? RedeId { get; set; }
    public Guid? PacoteId { get; set; }
    public string? Chave { get; set; }
    public bool Resumo { get; set; }
    public int? Folga { get; set; }
    public bool Critica { get; set; }

    public bool Marco => !Resumo && Duracao == 0;
    public bool Concluida => Avanco >= 100;
}
