namespace Sigeo.Domain.Common;

public enum PerfilUsuario { Administrador = 1, Diretoria, Engenharia, Qualidade, Suprimentos, Financeiro, Campo, Terceiro, Almoxarifado, Seguranca }

public enum StatusAprovacao { Rascunho = 0, EmAprovacao = 1, Aprovado = 2, Reprovado = 3, Cancelado = 4 }

public enum DecisaoAprovacao { Pendente = 0, Aprovado = 1, Reprovado = 2, DevolvidoParaAjuste = 3 }

/// <summary>Objetos que passam pelo motor de aprovação (alçadas por tipo × obra × valor).</summary>
public enum EntidadeAprovavel
{
    Pedido = 1, Medicao = 2, DocumentoSgq = 3, AvaliacaoFornecedor = 4,
    Requisicao = 5, Cotacao = 6, Contrato = 7, Pagamento = 8, Aditivo = 9, Cronograma = 10
}
