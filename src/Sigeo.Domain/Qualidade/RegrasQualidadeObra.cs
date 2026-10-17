using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Regras transversais ═══════════════════════

public static class RegrasQualidadeObra
{
    /// <summary>Lote com quarentena só é aplicado depois dos ensaios aprovados; soma aplicada ≤ recebida.</summary>
    public static IReadOnlyList<string> PodeAplicar(StatusLote status, bool quarentena, IEnumerable<EnsaioRecebimento> ensaios,
        decimal recebido, decimal jaAplicado, decimal quantidade)
    {
        var e = new List<string>();
        if (quantidade <= 0) e.Add("Quantidade aplicada deve ser positiva.");
        if (status is StatusLote.Devolvido) e.Add("Lote devolvido não pode ser aplicado.");
        if (quarentena && status == StatusLote.EmQuarentena && ensaios.Any(x => x.Situacao != SituacaoEnsaio.Aprovado))
            e.Add("Lote em quarentena: aguarde a aprovação dos ensaios.");
        if (ensaios.Any(x => x.Situacao == SituacaoEnsaio.Reprovado)) e.Add("Lote com ensaio reprovado: trate a RNC antes de aplicar.");
        if (jaAplicado + quantidade > recebido) e.Add($"Soma aplicada ({jaAplicado + quantidade:0.###}) passa da quantidade recebida ({recebido:0.###}).");
        return e;
    }

    /// <summary>Status do lote a partir dos ensaios (quarentena só libera com todos aprovados).</summary>
    public static StatusLote StatusDoLote(bool fvmAprovada, bool devolvido, bool concessao, bool quarentena, IReadOnlyCollection<EnsaioRecebimento> ensaios)
    {
        if (devolvido) return StatusLote.Devolvido;
        if (concessao) return StatusLote.AceitoSobConcessao;
        if (!fvmAprovada) return StatusLote.EmVerificacao;
        if (quarentena && (ensaios.Count == 0 || ensaios.Any(x => x.Situacao != SituacaoEnsaio.Aprovado))) return StatusLote.EmQuarentena;
        return StatusLote.Liberado;
    }

    /// <summary>Etapa bloqueada: FVS do serviço anterior (cujo modelo aponta este serviço como seguinte) reprovada no mesmo local.</summary>
    public static string? EtapaBloqueada(Guid localId, Guid servicoId, IEnumerable<(Fvs Fvs, Guid? ServicoSeguinteId)> fvsDoLocal)
    {
        var bloqueio = fvsDoLocal.FirstOrDefault(x => x.Fvs.LocalId == localId && x.ServicoSeguinteId == servicoId && x.Fvs.BloqueiaEtapaSeguinte);
        return bloqueio.Fvs is null ? null : $"Etapa bloqueada pela {bloqueio.Fvs.Numero} reprovada neste local — aguarde a reinspeção.";
    }
}
