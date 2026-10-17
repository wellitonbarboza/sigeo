using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

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
