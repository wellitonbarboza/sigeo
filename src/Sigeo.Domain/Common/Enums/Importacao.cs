namespace Sigeo.Domain.Common;

public enum TipoImportacao { Fornecedores = 1, Funcionarios = 2, Pedidos = 3, Medicoes = 4, Servicos = 5, Materiais = 6, Produtos = 7, HistoricoObraErp = 8 }

public enum StatusImportacao { Enviada = 1, EmValidacao = 2, ComErros = 3, AguardandoConfirmacao = 4, Concluida = 5, Cancelada = 6 }

public enum AcaoLinhaImportacao { Inserir = 1, Atualizar = 2, SemAlteracao = 3, Erro = 4 }

/// <summary>Listas configuráveis pelo usuário (tabela ItemLista). Carga inicial genérica, editável na tela.</summary>
public enum TipoLista
{
    Atividade = 1, CategoriaServico = 2, CriterioQualificacao = 3, ProcessoArea = 4, TipoDocumentoSgq = 5,
    EmissorExterno = 6, CondicaoPagamento = 7, Funcao = 8, TipoTarefa = 9, TipoAgrupamento = 10,
    TipoComodo = 11, GrupoServico = 12, CategoriaDocumento = 13,
    Disciplina = 14, ModoFalha = 15, MotivoAjusteEstoque = 16
}

public enum PrioridadeTarefa { Baixa, Normal, Alta, Urgente }

public enum StatusTarefa { Aberta, EmAndamento, Concluida, Cancelada }
