using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Planejamento;

/// <summary>
/// Restrição que impede uma atividade do cronograma de começar (projeto, material, mão de obra, contrato…).
/// Ligada à atividade pela chave estável (rede|pacote|local) para continuar valendo entre reprogramações.
/// Atividade com restrição aberta não entra no plano semanal.
/// </summary>
public class Restricao : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public TipoRestricao Tipo { get; set; } = TipoRestricao.Material;
    public string Descricao { get; set; } = string.Empty;
    /// <summary>Chave estável da atividade (Chave do gerador; para atividade manual, "A:{id}").</summary>
    public string AtividadeChave { get; set; } = string.Empty;
    public string AtividadeNome { get; set; } = string.Empty;
    public DateOnly? InicioPrevisto { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    /// <summary>Data até a qual a restrição precisa estar removida (normalmente antes do início da atividade).</summary>
    public DateOnly DataLimite { get; set; }
    public StatusRestricao Status { get; set; } = StatusRestricao.Aberta;
    public string? Resolucao { get; set; }
    public DateTimeOffset? RemovidaEm { get; set; }
    public string? RemovidaPor { get; set; }
    /// <summary>Documento que gerou a restrição automaticamente (transferência de material, OS de equipamento): ao concluir, ela é removida sozinha.</summary>
    public Guid? OrigemId { get; set; }

    public bool Aberta => Status == StatusRestricao.Aberta;
    public bool Vencida(DateOnly hoje) => Aberta && DataLimite < hoje;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a restrição.");
        if (string.IsNullOrWhiteSpace(AtividadeChave)) e.Add("Escolha a atividade do cronograma.");
        if (string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável por remover a restrição.");
        if (DataLimite == default) e.Add("Informe a data-limite.");
        return e;
    }

    public void Remover(string comoFoiRemovida, string usuario, DateTimeOffset agora)
    {
        if (!Aberta) throw new RegraDeNegocioException("Restrição já encerrada.");
        if (string.IsNullOrWhiteSpace(comoFoiRemovida)) throw new RegraDeNegocioException("Diga como a restrição foi removida.");
        Status = StatusRestricao.Removida; Resolucao = comoFoiRemovida.Trim(); RemovidaEm = agora; RemovidaPor = usuario;
    }

    public void Cancelar(string motivo, string usuario, DateTimeOffset agora)
    {
        if (!Aberta) throw new RegraDeNegocioException("Restrição já encerrada.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo do cancelamento.");
        Status = StatusRestricao.Cancelada; Resolucao = $"Cancelada: {motivo.Trim()}"; RemovidaEm = agora; RemovidaPor = usuario;
    }
}
