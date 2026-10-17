using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public enum SituacaoProposta
{
    ConviteRecebido = 1, EmPreenchimento = 2, PropostaEnviada = 3, Declinada = 4, PrazoEncerrado = 5,
    EmAnalise = 6, Vencedora = 7, VencedoraParcial = 8, NaoVencedora = 9, Cancelada = 10
}

/// <summary>Situação da cotação vista pelo fornecedor no portal (nunca mostra preço ou nome de concorrente).</summary>
public static class SituacaoPropostaFornecedor
{
    public sealed record Resultado(SituacaoProposta Situacao, string Texto, string Tom, bool PodeEditar);

    public static bool Aberta(StatusCotacao s) => s is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao;

    /// <param name="itensVencidos">itens que o fornecedor ganhou (só conta depois da escolha do vencedor)</param>
    /// <param name="itensTotal">itens da cotação para o fornecedor</param>
    /// <param name="decidida">a construtora já escolheu os vencedores (aprovação em andamento ou concluída)</param>
    public static Resultado Calcular(StatusCotacao cotacao, DateTimeOffset? prazo, CotacaoFornecedor conv, int itensVencidos, int itensTotal, bool decidida, DateTimeOffset agora)
    {
        if (cotacao == StatusCotacao.Cancelado) return new(SituacaoProposta.Cancelada, "Cancelada", "neutro", false);
        var aberta = Aberta(cotacao) && (prazo is null || prazo > agora);
        var enviada = conv.Status is StatusConvite.Respondido or StatusConvite.Vencedor;
        if (decidida || cotacao is StatusCotacao.Aprovado or StatusCotacao.PedidoGerado)
        {
            if (!enviada) return conv.Status == StatusConvite.Declinou ? new(SituacaoProposta.Declinada, "Declinada", "neutro", false) : new(SituacaoProposta.PrazoEncerrado, "Prazo encerrado", "erro", false);
            if (itensVencidos > 0 && itensVencidos >= itensTotal) return new(SituacaoProposta.Vencedora, "Vencedora", "ok", false);
            if (itensVencidos > 0) return new(SituacaoProposta.VencedoraParcial, $"Vencedora parcial ({itensVencidos} de {itensTotal} itens)", "ok", false);
            return new(SituacaoProposta.NaoVencedora, "Não vencedora", "neutro", false);
        }
        if (conv.Status == StatusConvite.Declinou) return new(SituacaoProposta.Declinada, "Declinada", "neutro", aberta);
        if (!aberta)
            return enviada ? new(SituacaoProposta.EmAnalise, "Em análise", "info", false) : new(SituacaoProposta.PrazoEncerrado, "Prazo encerrado", "erro", false);
        if (enviada) return new(SituacaoProposta.PropostaEnviada, $"Proposta enviada{(conv.Versoes.Count > 1 ? $" · versão {conv.Versoes.Count}" : "")}", "ok", true);
        if (conv.RascunhoEm is not null) return new(SituacaoProposta.EmPreenchimento, "Em preenchimento", "aviso", true);
        return new(SituacaoProposta.ConviteRecebido, "Convite recebido", "aviso", true);
    }
}

public enum SituacaoCompetencia { Aberta = 1, Enviada = 2, EmConferencia = 3, ComPendencia = 4, Conferida = 5 }
public enum SituacaoItemCompetencia { Pendente = 1, Enviado = 2, Aprovado = 3, Recusado = 4, AdiamentoSolicitado = 5, Adiado = 6 }

/// <summary>
/// Documentação mensal de um contrato de mão de obra (competência = mês trabalhado). O fornecedor envia na competência,
/// a construtora confere item a item e a medição daquele mês só é aprovada com a competência conferida.
/// </summary>
public class CompetenciaDocumental : EntidadeDaObra
{
    public Guid ContratoId { get; set; }
    public Guid FornecedorId { get; set; }
    /// <summary>1º dia do mês trabalhado.</summary>
    public DateOnly Mes { get; set; }
    public SituacaoCompetencia Situacao { get; set; } = SituacaoCompetencia.Aberta;
    public DateTimeOffset? EnviadaEm { get; set; }
    public string? EnviadaPor { get; set; }
    public DateTimeOffset? ConferidaEm { get; set; }
    public string? ConferidaPor { get; set; }
    public string? MotivoReabertura { get; set; }
    public ICollection<ItemCompetencia> Itens { get; set; } = [];

    /// <summary>Dia limite de entrega (padrão: dia 20 do mês seguinte ao trabalhado).</summary>
    public DateOnly Limite(int dia = 20) => Mes.AddMonths(1).AddDays(Math.Clamp(dia, 1, 28) - 1);
    public bool Atrasada(DateOnly hoje, int dia = 20) => Situacao != SituacaoCompetencia.Conferida && hoje > Limite(dia);
    public int Entregues => Itens.Count(i => i.Situacao is SituacaoItemCompetencia.Enviado or SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Adiado);
    /// <summary>Medição liberada: conferida (itens adiados com aprovação não travam).</summary>
    public bool LiberaMedicao => Situacao == SituacaoCompetencia.Conferida;

    /// <summary>Marca como completa (fornecedor): todo item enviado, ou adiado/adiamento pedido.</summary>
    public void Enviar(string quem, DateTimeOffset agora)
    {
        if (Situacao is SituacaoCompetencia.Conferida) throw new Aprovacoes.RegraDeNegocioException("Competência já conferida.");
        var faltam = Itens.Where(i => i.Situacao is SituacaoItemCompetencia.Pendente or SituacaoItemCompetencia.Recusado).ToList();
        if (faltam.Count > 0) throw new Aprovacoes.RegraDeNegocioException($"Faltam {faltam.Count} item(ns): {string.Join(", ", faltam.Take(5).Select(f => f.Descricao))}{(faltam.Count > 5 ? "…" : "")}.");
        Situacao = SituacaoCompetencia.Enviada; EnviadaEm = agora; EnviadaPor = quem;
    }

    /// <summary>Conclui a conferência: todos aprovados (ou adiados com aprovação do engenheiro).</summary>
    public void Concluir(string quem, DateTimeOffset agora)
    {
        var abertos = Itens.Where(i => i.Situacao is not (SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Adiado)).ToList();
        if (abertos.Count > 0) throw new Aprovacoes.RegraDeNegocioException($"Há {abertos.Count} item(ns) sem aprovação: {string.Join(", ", abertos.Take(5).Select(a => a.Descricao))}.");
        Situacao = SituacaoCompetencia.Conferida; ConferidaEm = agora; ConferidaPor = quem;
    }

    public void Devolver(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) && !Itens.Any(i => i.Situacao == SituacaoItemCompetencia.Recusado))
            throw new Aprovacoes.RegraDeNegocioException("Recuse os itens com o motivo ou informe o motivo da devolução.");
        Situacao = SituacaoCompetencia.ComPendencia;
        MotivoReabertura = motivo;
    }

    public void Reabrir(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Reabrir competência conferida exige motivo.");
        Situacao = SituacaoCompetencia.ComPendencia; MotivoReabertura = motivo.Trim(); ConferidaEm = null; ConferidaPor = null;
    }
}

public class ItemCompetencia : Entidade
{
    public Guid CompetenciaId { get; set; }
    public Guid TipoDocumentoExigidoId { get; set; }
    /// <summary>Documento por funcionário (holerite, ponto, EPI…); nulo = documento da empresa.</summary>
    public Guid? ColaboradorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool PodeAdiar { get; set; }
    public SituacaoItemCompetencia Situacao { get; set; } = SituacaoItemCompetencia.Pendente;
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? EnviadoPor { get; set; }
    /// <summary>Documento do fornecedor gerado pelo envio (documentos da empresa), para o cadastro e relatórios.</summary>
    public Guid? FornecedorDocumentoId { get; set; }
    public string? MotivoRecusa { get; set; }
    public string? ConferidoPor { get; set; }
    public DateTimeOffset? ConferidoEm { get; set; }
    public string? JustificativaAdiamento { get; set; }
    public DateOnly? AdiadoAte { get; set; }
    public string? AdiamentoAprovadoPor { get; set; }
    public DateTimeOffset? AdiamentoAprovadoEm { get; set; }

    public void Enviado(string blob, string nome, string quem, DateTimeOffset agora)
    {
        if (Situacao == SituacaoItemCompetencia.Aprovado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: já aprovado.");
        ArquivoBlob = blob; ArquivoNome = nome; EnviadoPor = quem; EnviadoEm = agora; MotivoRecusa = null;
        Situacao = SituacaoItemCompetencia.Enviado;
    }

    public void Conferir(bool aprovado, string? motivo, string quem, DateTimeOffset agora)
    {
        if (Situacao is not (SituacaoItemCompetencia.Enviado or SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Recusado))
            throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: nada enviado para conferir.");
        if (!aprovado && string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: informe o motivo da recusa.");
        Situacao = aprovado ? SituacaoItemCompetencia.Aprovado : SituacaoItemCompetencia.Recusado;
        MotivoRecusa = aprovado ? null : motivo!.Trim();
        ConferidoPor = quem; ConferidoEm = agora;
    }

    public void SolicitarAdiamento(string? justificativa, DateOnly? ate, DateOnly hoje)
    {
        if (!PodeAdiar) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: este documento não pode ser adiado.");
        if (Situacao is SituacaoItemCompetencia.Aprovado or SituacaoItemCompetencia.Enviado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: já enviado.");
        if (string.IsNullOrWhiteSpace(justificativa)) throw new Aprovacoes.RegraDeNegocioException("Informe a justificativa do adiamento.");
        if (ate is not { } d || d <= hoje) throw new Aprovacoes.RegraDeNegocioException("Informe a nova data de entrega (futura).");
        if (d > hoje.AddDays(60)) throw new Aprovacoes.RegraDeNegocioException("Adiamento de no máximo 60 dias.");
        JustificativaAdiamento = justificativa.Trim(); AdiadoAte = d;
        Situacao = SituacaoItemCompetencia.AdiamentoSolicitado;
        AdiamentoAprovadoPor = null; AdiamentoAprovadoEm = null;
    }

    /// <summary>Engenheiro da obra aprova (item fica "Adiado até" e não trava a medição) ou recusa (volta a pendente).</summary>
    public void DecidirAdiamento(bool aprovado, string? motivo, string quem, DateTimeOffset agora)
    {
        if (Situacao != SituacaoItemCompetencia.AdiamentoSolicitado) throw new Aprovacoes.RegraDeNegocioException($"{Descricao}: não há pedido de adiamento.");
        if (!aprovado && string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Informe o motivo da recusa do adiamento.");
        if (aprovado) { Situacao = SituacaoItemCompetencia.Adiado; AdiamentoAprovadoPor = quem; AdiamentoAprovadoEm = agora; }
        else { Situacao = SituacaoItemCompetencia.Pendente; MotivoRecusa = $"Adiamento recusado: {motivo!.Trim()}"; AdiadoAte = null; }
    }

    /// <summary>Adiado e passou da nova data sem entrega: volta a travar a próxima medição.</summary>
    public bool AdiamentoVencido(DateOnly hoje) => Situacao == SituacaoItemCompetencia.Adiado && AdiadoAte is { } a && a < hoje;
}

/// <summary>Regras da documentação mensal ligadas à medição.</summary>
public static class RegrasCompetencia
{
    /// <summary>Bloqueios da medição: competência do mês não conferida e adiamentos vencidos sem entrega no contrato.</summary>
    public static IEnumerable<string> BloqueiosMedicao(DateOnly mesMedicao, IReadOnlyCollection<CompetenciaDocumental> doContrato, DateOnly hoje)
    {
        var mes = new DateOnly(mesMedicao.Year, mesMedicao.Month, 1);
        var c = doContrato.FirstOrDefault(x => x.Mes == mes);
        if (c is not null && c.Itens.Count > 0 && !c.LiberaMedicao)
            yield return $"Documentação mensal {mes:MM/yyyy} não conferida ({c.Entregues} de {c.Itens.Count} itens entregues).";
        foreach (var i in doContrato.SelectMany(x => x.Itens.Select(i => (x.Mes, i))).Where(t => t.i.AdiamentoVencido(hoje)))
            yield return $"Documento adiado vencido: {i.i.Descricao} ({i.Mes:MM/yyyy}), prometido para {i.i.AdiadoAte:dd/MM/yyyy}.";
    }

    /// <summary>Meses de competência do contrato até o mês anterior a hoje (competência do mês M abre no dia 1 do mês M+1).</summary>
    public static IEnumerable<DateOnly> Meses(DateOnly inicioContrato, DateOnly? fimContrato, DateOnly hoje)
    {
        var m = new DateOnly(inicioContrato.Year, inicioContrato.Month, 1);
        var ultimo = new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1);
        if (fimContrato is { } f && new DateOnly(f.Year, f.Month, 1) < ultimo) ultimo = new DateOnly(f.Year, f.Month, 1);
        for (; m <= ultimo; m = m.AddMonths(1)) yield return m;
    }
}
