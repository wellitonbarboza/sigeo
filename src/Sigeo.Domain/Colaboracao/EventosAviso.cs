using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>Nomes dos eventos das regras de notificação (o texto é o mesmo da tela de Parâmetros e Notificações).</summary>
public static class EventosAviso
{
    public const string AprovacaoPendente = "Aprovação pendente";
    public const string PrazoAprovacaoEstourado = "Prazo de aprovação estourado";
    public const string NovaRevisao = "Nova revisão publicada";
    public const string RncVencida = "RNC / ação corretiva vencida";
    public const string NovaMensagem = "Nova mensagem interna";
    public const string TarefaAtribuida = "Tarefa atribuída";
    public const string TarefaAtrasada = "Tarefa atrasada";
    public const string RestricaoVencendo = "Restrição do planejamento vencendo";
    public const string DocumentoFornecedorVencendo = "Documento do fornecedor vencendo";
    public const string DocumentoRecusado = "Documento recusado";
    /// <summary>Resumo diário ao fornecedor: documentos vencendo/vencidos, documentação mensal e cotação fechando (um e-mail por dia).</summary>
    public const string PendenciasFornecedor = "Pendências do fornecedor (resumo diário)";
    /// <summary>Aviso interno: documentação mensal atrasada / adiamento aguardando o engenheiro.</summary>
    public const string DocumentacaoMensal = "Documentação mensal do fornecedor";
}
