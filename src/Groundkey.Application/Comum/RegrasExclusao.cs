using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Suprimentos;

namespace Groundkey.Application.Comum;

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
        Domain.Engenharia.CustoRealizado { Origem: not OrigemCusto.Manual } => "Custo que veio do recebimento, da medição ou do ERP não é excluído aqui: reclassifique fase/local ou estorne o documento de origem.",
        _ => null
    };
}
