using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>Ordem de serviço de manutenção (preventiva, corretiva, inspeção).</summary>
public class OrdemServico : EntidadeAuditavel, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid EquipamentoId { get; set; }
    public Guid? PlanoId { get; set; }
    public Guid? ContratoId { get; set; }
    public TipoOrdemServico Tipo { get; set; } = TipoOrdemServico.Corretiva;
    public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Aberta;
    public DateTimeOffset AbertaEm { get; set; }
    public string AbertaPor { get; set; } = string.Empty;
    public DateTimeOffset? ParadoDesde { get; set; }
    public DateTimeOffset? ParadoAte { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? ModoFalha { get; set; }
    public string? Causa { get; set; }
    public decimal CustoMaoObra { get; set; }
    public decimal CustoPecas { get; set; }
    public decimal CustoTerceiros { get; set; }
    /// <summary>Resultado do checklist ("item=OK|Ajustado|Trocar", uma linha por item).</summary>
    public string? ChecklistResultado { get; set; }
    public decimal? Leitura { get; set; }
    public string? ExecutadoPor { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
    public string? VerificadoPor { get; set; }
    public DateTimeOffset? VerificadaEm { get; set; }
    public Guid? RequisicaoId { get; set; }
    public string? Observacao { get; set; }

    public decimal CustoTotal => CustoMaoObra + CustoPecas + CustoTerceiros;
    public bool EmAberto => Status is StatusOrdemServico.Aberta or StatusOrdemServico.EmExecucao or StatusOrdemServico.AguardandoPeca;
    public double HorasParado(DateTimeOffset agora) => ParadoDesde is { } d ? ((ParadoAte ?? agora) - d).TotalHours : 0;

    public void Avancar(StatusOrdemServico novo, string usuario, DateTimeOffset agora)
    {
        var ok = (Status, novo) switch
        {
            (StatusOrdemServico.Aberta, StatusOrdemServico.EmExecucao) => true,
            (StatusOrdemServico.EmExecucao, StatusOrdemServico.AguardandoPeca) => true,
            (StatusOrdemServico.AguardandoPeca, StatusOrdemServico.EmExecucao) => true,
            (StatusOrdemServico.EmExecucao, StatusOrdemServico.Concluida) => true,
            (StatusOrdemServico.Concluida, StatusOrdemServico.Verificada) => true,
            (StatusOrdemServico.Concluida, StatusOrdemServico.EmExecucao) => true, // verificação reprovou
            (StatusOrdemServico.Aberta or StatusOrdemServico.EmExecucao or StatusOrdemServico.AguardandoPeca, StatusOrdemServico.Cancelada) => true,
            _ => false,
        };
        if (!ok) throw new RegraDeNegocioException($"A OS não pode passar de {Status} para {novo}.");
        if (novo == StatusOrdemServico.Concluida)
        {
            if (Tipo == TipoOrdemServico.Corretiva && string.IsNullOrWhiteSpace(Causa)) throw new RegraDeNegocioException("Corretiva: informe a causa antes de concluir.");
            ConcluidaEm = agora; ExecutadoPor ??= usuario;
            if (ParadoDesde is not null) ParadoAte ??= agora;
        }
        if (novo == StatusOrdemServico.Verificada)
        {
            if (string.Equals(ExecutadoPor, usuario, StringComparison.OrdinalIgnoreCase) && Tipo == TipoOrdemServico.Corretiva)
                throw new RegraDeNegocioException("A verificação deve ser feita por outra pessoa (não por quem executou).");
            VerificadoPor = usuario; VerificadaEm = agora;
        }
        Status = novo;
    }
}
