using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

// ═══════════════════════ S11/S12 — Permissão de trabalho ═══════════════════════

public class PermissaoTrabalho : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public TipoPermissaoTrabalho Tipo { get; set; }
    public Guid LocalId { get; set; }
    public string Atividade { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fim { get; set; }
    public string Medidas { get; set; } = string.Empty;          // itens separados por ';'
    public string MedidasMarcadas { get; set; } = string.Empty;  // índices marcados separados por ';'
    public string Emitente { get; set; } = string.Empty;
    public string ResponsavelArea { get; set; } = string.Empty;
    public StatusPermissaoTrabalho Status { get; set; } = StatusPermissaoTrabalho.Rascunho;
    public DateTimeOffset? EncerradaEm { get; set; }
    public string? EncerradaPor { get; set; }
    public ICollection<ExecutantePt> Executantes { get; set; } = [];

    public static AtividadeRisco Risco(TipoPermissaoTrabalho t) => t switch
    {
        TipoPermissaoTrabalho.Altura => AtividadeRisco.Altura,
        TipoPermissaoTrabalho.AQuente => AtividadeRisco.AQuente,
        TipoPermissaoTrabalho.Eletrico => AtividadeRisco.Eletrico,
        TipoPermissaoTrabalho.EspacoConfinado => AtividadeRisco.EspacoConfinado,
        _ => AtividadeRisco.Icamento
    };

    public static string MedidasPadrao(TipoPermissaoTrabalho t) => t switch
    {
        TipoPermissaoTrabalho.Altura => "Linha de vida / ancoragem inspecionada;Cinto paraquedista com talabarte duplo;Área abaixo isolada e sinalizada;Condições de vento e chuva verificadas;Plano de resgate e equipe disponível",
        TipoPermissaoTrabalho.AQuente => "Área livre de inflamáveis;Extintor no local;Vigia de fogo designado;Biombo / anteparo instalado;Inspeção pós-trabalho (30 min)",
        TipoPermissaoTrabalho.Eletrico => "Circuito desenergizado e bloqueado (LOTO);Ausência de tensão testada;Aterramento temporário;Ferramentas isoladas;Área sinalizada",
        TipoPermissaoTrabalho.EspacoConfinado => "Atmosfera medida (O2, inflamáveis, tóxicos);Ventilação instalada;Vigia designado;Equipe de resgate disponível;Comunicação testada",
        _ => "Plano de rigging aprovado;Equipamento e acessórios inspecionados;Área isolada;Sinaleiro designado;Condições de vento verificadas"
    };

    public IReadOnlyList<string> Liberar(DateTimeOffset agora)
    {
        var e = new List<string>();
        if (Status is not (StatusPermissaoTrabalho.Rascunho or StatusPermissaoTrabalho.AguardandoLiberacao or StatusPermissaoTrabalho.Suspensa)) e.Add("PT não pode ser liberada neste status.");
        var total = Medidas.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        var marcadas = MedidasMarcadas.Split(';', StringSplitOptions.RemoveEmptyEntries).Distinct().Count();
        if (total == 0 || marcadas < total) e.Add("Marque todas as medidas de controle para liberar.");
        if (Fim <= Inicio) e.Add("Validade: fim deve ser posterior ao início.");
        if ((Fim - Inicio).TotalHours > 14) e.Add("Validade máxima: um turno do dia.");
        if (Fim < agora) e.Add("Validade já encerrada.");
        if (string.IsNullOrWhiteSpace(Emitente) || string.IsNullOrWhiteSpace(ResponsavelArea)) e.Add("Emitente e responsável da área devem assinar.");
        if (Executantes.Count == 0) e.Add("Inclua os executantes.");
        if (Executantes.Any(x => !x.Liberado)) e.Add("Há executante que não pode entrar: retire-o ou regularize as exigências.");
        if (e.Count == 0) Status = StatusPermissaoTrabalho.Liberada;
        return e;
    }
}
