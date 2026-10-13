using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Application.Comum;

/// <summary>Regras de situação para excluir documentos (além do "está em uso"): só rascunho / em elaboração.</summary>
public static class RegrasExclusao
{
    public static string? Validar(object entidade) => entidade switch
    {
        Contrato { Status: not StatusContrato.EmElaboracao } => "Contrato fora de elaboração não é excluído: encerre ou rescinda.",
        Requisicao { Status: not (StatusRequisicao.Rascunho or StatusRequisicao.DevolvidaAjuste) } => "Requisição já enviada não é excluída: cancele.",
        Pedido => "Pedido não é excluído: cancele.",
        Medicao { Status: not StatusMedicao.EmElaboracao } => "Medição já enviada não é excluída.",
        Domain.Qualidade.ModeloInspecao { Situacao: not (SituacaoModelo.Rascunho or SituacaoModelo.EmRevisao) } => "Modelo vigente/obsoleto não é excluído: torne obsoleto.",
        TransferenciaMaterial { Status: not StatusTransferencia.Rascunho } => "Transferência enviada não é excluída: cancele.",
        InventarioEstoque => "Inventário não é excluído: cancele.",
        OrdemServico => "OS não é excluída: cancele.",
        ParcelaContrato => "Parcela não é excluída: cancele ou devolva.",
        Usuario { Perfil: PerfilUsuario.Administrador } u when u.Ativo => "Administrador ativo não é excluído: inative antes (e garanta outro administrador).",
        _ => null
    };
}
