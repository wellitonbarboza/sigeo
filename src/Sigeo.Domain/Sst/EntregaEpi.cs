using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Domain.Sst;

public class EntregaEpi : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid ColaboradorId { get; set; }
    public Guid EpiId { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public DateOnly DataEntrega { get; set; }
    public MotivoEntregaEpi Motivo { get; set; } = MotivoEntregaEpi.PrimeiraEntrega;
    public DateOnly? TrocaPrevista { get; set; }
    public string Assinatura { get; set; } = string.Empty;
    public string EntreguePor { get; set; } = string.Empty;

    public static EntregaEpi Registrar(Epi epi, Guid colaboradorId, decimal qtd, DateOnly data, MotivoEntregaEpi motivo, string assinatura, string por)
    {
        if (epi.ValidadeCa < data) throw new RegraDeNegocioException($"CA {epi.Ca} vencido em {epi.ValidadeCa:dd/MM/yyyy}: EPI não pode ser entregue (NR-06).");
        if (qtd <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
        if (string.IsNullOrWhiteSpace(assinatura)) throw new RegraDeNegocioException("Colete a assinatura do colaborador.");
        return new EntregaEpi
        {
            ColaboradorId = colaboradorId, EpiId = epi.Id, Quantidade = qtd, DataEntrega = data, Motivo = motivo, Assinatura = assinatura, EntreguePor = por,
            TrocaPrevista = epi.VidaUtilDias is { } d ? data.AddDays(d) : null
        };
    }
}
