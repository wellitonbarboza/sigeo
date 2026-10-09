using QualyHub.Domain.Common;

namespace QualyHub.Application.Comum;

/// <summary>
/// Endereço (relativo) da tela de cada registro: usado nos links das mensagens, dos e-mails de aviso e das pendências.
/// O nome da entidade é o mesmo usado nas notas e anexos (ex.: "Rnc", "Pedido").
/// </summary>
public static class RotasRegistro
{
    private static readonly Dictionary<string, (string Rota, string Rotulo)> Mapa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rnc"] = ("obra/rnc", "RNC"), ["AcaoCorretiva"] = ("obra/acoes", "Ação corretiva"), ["Auditoria"] = ("obra/auditorias", "Auditoria"),
        ["Fvs"] = ("obra/fvs", "FVS"), ["PlanoAcao"] = ("fornecedores/planos", "Plano de ação"),
        ["Requisicao"] = ("requisicoes", "Requisição"), ["Cotacao"] = ("cotacoes", "Cotação"), ["Pedido"] = ("pedidos", "Pedido"),
        ["Contrato"] = ("contratos", "Contrato"), ["Medicao"] = ("medicoes", "Medição"), ["Recebimento"] = ("recebimentos", "Recebimento"),
        ["Pagamento"] = ("pagamentos", "Liberação de pagamento"), ["Tarefa"] = ("tarefas", "Tarefa"),
        ["Conversa"] = ("mensagens", "Mensagem"), ["Cronograma"] = ("engenharia/planejamento", "Cronograma"), ["Restricao"] = ("engenharia/planejamento", "Restrição"),
    };

    public static string Rotulo(string? entidade) => entidade is not null && Mapa.TryGetValue(entidade, out var m) ? m.Rotulo : entidade ?? "";

    public static string? Link(string? entidade, Guid? id)
    {
        if (entidade is null || id is not { } i || !Mapa.TryGetValue(entidade, out var m)) return null;
        return entidade switch
        {
            "Cronograma" => $"engenharia/planejamento?aba=versoes&versao={i}",
            "Restricao" => $"engenharia/planejamento?aba=medio&restricao={i}",
            _ => $"{m.Rota}?id={i}"
        };
    }

    public static string Link(EntidadeAprovavel e, Guid id) => e switch
    {
        EntidadeAprovavel.Requisicao => $"requisicoes?id={id}", EntidadeAprovavel.Cotacao => $"cotacoes?id={id}", EntidadeAprovavel.Pedido => $"pedidos?id={id}",
        EntidadeAprovavel.Contrato => $"contratos?id={id}", EntidadeAprovavel.Medicao => $"medicoes?id={id}", EntidadeAprovavel.Pagamento => $"pagamentos?id={id}",
        EntidadeAprovavel.Aditivo => $"contratos?aditivo={id}", EntidadeAprovavel.DocumentoSgq => $"sgq/documentos?id={id}",
        EntidadeAprovavel.Cronograma => "engenharia/planejamento?aba=versoes", EntidadeAprovavel.AvaliacaoFornecedor => "fornecedores/avaliacao",
        _ => ""
    };
}
