using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Q06/Q07 — RNC e ação corretiva ═══════════════════════

/// <summary>Q06 · Registro de não conformidade.</summary>
public class Rnc : EntidadeAuditavel, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public OrigemRnc Origem { get; set; }
    public string? Referencia { get; set; }
    public Guid? ReferenciaId { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? LocalId { get; set; }
    public string? LocaisAfetados { get; set; }
    public Guid? FornecedorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public ClassificacaoRnc Classificacao { get; set; } = ClassificacaoRnc.Servico;
    public Gravidade Gravidade { get; set; } = Gravidade.Media;
    public string? Disposicao { get; set; }
    public string? Concessao { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly Prazo { get; set; }
    public bool ExigeAcao { get; set; }
    public Guid? AcaoCorretivaId { get; set; }
    public StatusRnc Status { get; set; } = StatusRnc.Aberta;
    public string? EncerradaPor { get; set; }
    public DateTimeOffset? EncerradaEm { get; set; }

    public bool Vencida(DateOnly hoje) => Status is StatusRnc.Aberta or StatusRnc.EmTratamento && Prazo < hoje;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a não conformidade.");
        if (string.IsNullOrWhiteSpace(Disposicao)) e.Add("Informe a disposição (tratamento imediato).");
        if (Disposicao?.Contains("concess", StringComparison.OrdinalIgnoreCase) == true && string.IsNullOrWhiteSpace(Concessao))
            e.Add("Aceite sob concessão: registre a aprovação do projetista / Diretoria.");
        if (ResponsavelId is null && string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável.");
        if (Gravidade >= Gravidade.Alta && !ExigeAcao) e.Add("Gravidade Alta exige ação corretiva.");
        return e;
    }

    /// <summary>Gravidade alta ou reincidência (mesmo serviço/prestador no período) obrigam ação corretiva.</summary>
    public void AvaliarExigencia(int reincidencias, int limiteReincidencia)
    {
        if (Gravidade >= Gravidade.Alta || reincidencias >= limiteReincidencia) ExigeAcao = true;
    }

    public void Encerrar(string por, DateTimeOffset agora, bool acaoEncerrada)
    {
        if (Status is StatusRnc.Encerrada or StatusRnc.Cancelada) throw new RegraDeNegocioException("RNC já encerrada.");
        if (ExigeAcao && AcaoCorretivaId is null) throw new RegraDeNegocioException("Esta RNC exige ação corretiva: abra a AC antes de encerrar.");
        if (ExigeAcao && !acaoEncerrada) { Status = StatusRnc.AguardandoEficacia; return; }
        Status = StatusRnc.Encerrada; EncerradaPor = por; EncerradaEm = agora;
    }
}
