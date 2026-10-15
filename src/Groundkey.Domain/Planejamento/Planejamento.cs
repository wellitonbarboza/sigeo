using Groundkey.Domain.Common;

namespace Groundkey.Domain.Planejamento;

/// <summary>Tipo do vínculo entre atividades (TI = término→início, II, TT, IT), como no MS Project (FS, SS, FF, SF).</summary>
public enum TipoVinculo { TerminoInicio = 0, InicioInicio = 1, TerminoTermino = 2, InicioTermino = 3 }

/// <summary>Situação da versão do cronograma. Só uma versão por obra fica Vigente (a aprovada mais recente).</summary>
public enum StatusVersao { EmElaboracao = 0, EmAprovacao = 1, Vigente = 2, Substituida = 3 }

/// <summary>Como a rede enxerga os locais: torre por apartamento, estrutura por pavimento, área comum por local.</summary>
public enum TipoRede { PorApartamento = 1, PorPavimento = 2, PorLocal = 3 }

/// <summary>
/// Vínculo entre pacotes de redes diferentes. PorPavimento: o destino no pavimento N espera a origem no pavimento N+deslocamento
/// (ex.: alvenaria do 3º espera a estrutura do 5º). AposFimDaRede: o destino só começa depois do último local da origem.
/// </summary>
public enum ModoVinculoRede { PorPavimento = 1, AposFimDaRede = 2 }

/// <summary>
/// Versão do cronograma da obra (LB0 = linha de base, R1, R2… = reprogramações). O engenheiro elabora e envia;
/// o diretor técnico aprova (motor de alçadas). Aprovada vira Vigente e grava o desembolso mensal; a anterior fica Substituída.
/// </summary>
public class VersaoCronograma : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public string Codigo { get; set; } = "LB0";
    public StatusVersao Status { get; set; } = StatusVersao.EmElaboracao;
    public string? Motivo { get; set; }
    public string? Descricao { get; set; }
    public DateOnly InicioProjeto { get; set; }
    public DateOnly? DataStatus { get; set; }
    public DateOnly? TerminoPrevisto { get; set; }
    public DateOnly? TerminoBase { get; set; }
    public decimal CustoTotal { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public string? MotivoDevolucao { get; set; }
    public ICollection<AtividadeCronograma> Atividades { get; set; } = [];
    public ICollection<VinculoAtividade> Vinculos { get; set; } = [];
    public ICollection<DesembolsoVersao> Desembolso { get; set; } = [];

    public static string CodigoDe(int numero) => numero == 0 ? "LB0" : $"R{numero}";
    public bool Editavel => Status == StatusVersao.EmElaboracao;
}

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

/// <summary>Vínculo entre duas atividades-folha da mesma versão, com folga (lag) em dias úteis (negativa = antecipação).</summary>
public class VinculoAtividade : Entidade
{
    public Guid VersaoId { get; set; }
    public Guid PredecessoraId { get; set; }
    public Guid SucessoraId { get; set; }
    public TipoVinculo Tipo { get; set; } = TipoVinculo.TerminoInicio;
    public int Lag { get; set; }
}

/// <summary>Desembolso mensal da versão (custo orçado distribuído pelos dias úteis de cada atividade), gravado na aprovação.</summary>
public class DesembolsoVersao : Entidade
{
    public Guid VersaoId { get; set; }
    public DateOnly Mes { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorBase { get; set; }
}

/// <summary>Feriado municipal/estadual ou parada da obra (os nacionais são calculados). ObraId nulo = vale para todas as obras.</summary>
public class Feriado : EntidadeAuditavel
{
    public DateOnly Data { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
}

/// <summary>
/// Rede de planejamento (conceito de redes por locais): conjunto de locais + pacotes de serviço em sequência.
/// Torre por apartamento, estrutura por pavimento, área comum por local. O cronograma é gerado a partir das redes.
/// </summary>
public class RedePlanejamento : EntidadeDaObra
{
    public string Nome { get; set; } = string.Empty;
    public TipoRede Tipo { get; set; } = TipoRede.PorApartamento;
    public string Cor { get; set; } = "#4f52d6";
    /// <summary>Pulmão (dias úteis) entre um pacote e o seguinte no mesmo local.</summary>
    public int Pulmao { get; set; }
    public int Ordem { get; set; }
    public ICollection<RedeLocal> Locais { get; set; } = [];
    public ICollection<RedePacote> Pacotes { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome da rede.");
        if (Pulmao is < 0 or > 60) e.Add("Pulmão entre 0 e 60 dias úteis.");
        if (Locais.Count == 0) e.Add("Inclua ao menos um local na rede.");
        if (Pacotes.Count == 0) e.Add("Inclua ao menos um pacote de serviço.");
        foreach (var p in Pacotes)
        {
            if (string.IsNullOrWhiteSpace(p.Nome)) e.Add("Pacote sem nome.");
            if (p.DuracaoPorLocal is < 0 or > 400) e.Add($"{p.Nome}: duração por local entre 0 e 400 dias úteis.");
            if (p.Equipes is < 1 or > 50) e.Add($"{p.Nome}: equipes entre 1 e 50.");
        }
        return e;
    }
}

/// <summary>Local incluído na rede (apartamento, pavimento ou área comum), na ordem de ataque.</summary>
public class RedeLocal : Entidade
{
    public Guid RedeId { get; set; }
    public Guid LocalId { get; set; }
    public int Ordem { get; set; }
}

/// <summary>Pacote de serviço da rede (normalmente um serviço do orçamento), com duração por local e nº de equipes.</summary>
public class RedePacote : Entidade
{
    public Guid RedeId { get; set; }
    public int Ordem { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ComposicaoId { get; set; }
    public int DuracaoPorLocal { get; set; } = 1;
    public int Equipes { get; set; } = 1;
    /// <summary>Ajustes do "o que está incluso": por padrão o pacote entra nos locais onde o orçamento tem o serviço (ou em todos, sem serviço).</summary>
    public ICollection<RedePacoteLocal> Ajustes { get; set; } = [];
}

/// <summary>Ajuste manual: pacote incluído/excluído num local (sobrepõe o padrão vindo do orçamento).</summary>
public class RedePacoteLocal : Entidade
{
    public Guid PacoteId { get; set; }
    public Guid LocalId { get; set; }
    public bool Incluido { get; set; }
}

/// <summary>
/// Vínculo entre pacotes de redes diferentes (ex.: alvenaria do pav. N após estrutura do pav. N+2; fachada após a alvenaria).
/// Filho dos pacotes (sai junto quando o pacote/rede sai), por isso não tem exclusão lógica; acesso sempre pela obra.
/// </summary>
public class VinculoRede : Entidade
{
    public Guid ObraId { get; set; }
    public Guid PacoteOrigemId { get; set; }
    public Guid PacoteDestinoId { get; set; }
    public ModoVinculoRede Modo { get; set; } = ModoVinculoRede.PorPavimento;
    public int Deslocamento { get; set; }
    public int Lag { get; set; }
}
