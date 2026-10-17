using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

// ═══════════════════════ Q08/Q09 — Equipamentos de medição ═══════════════════════

/// <summary>Q08 · Equipamento de medição (trena, nível, prumo…). Vencido ou reprovado não pode ser usado em FVS.</summary>
public class EquipamentoMedicao : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoEquipamento Tipo { get; set; } = TipoEquipamento.Trena;
    public TipoControleEquipamento TipoControle { get; set; } = TipoControleEquipamento.VerificacaoInterna;
    public Guid? PadraoId { get; set; }
    public int PeriodicidadeMeses { get; set; } = 6;
    public string Criterio { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
    public string? Responsavel { get; set; }
    public DateOnly? UltimaData { get; set; }
    public ResultadoCalibracao? UltimoResultado { get; set; }
    public string? UltimoCertificado { get; set; }
    public bool Baixado { get; set; }
    public bool EmCalibracao { get; set; }
    /// <summary>Tipo configurável (periodicidade, critério e controle padrão).</summary>
    public Guid? TipoMedicaoId { get; set; }
    /// <summary>Da construtora ou de terceiro (empreiteira).</summary>
    public DonoFerramenta Dono { get; set; } = DonoFerramenta.Propria;
    /// <summary>Empreiteira dona (terceiro).</summary>
    public Guid? FornecedorId { get; set; }
    /// <summary>Com quem está agora (cautela): colaborador, empreiteira ou setor.</summary>
    public string? ComQuem { get; set; }
    public Guid? CautelaColaboradorId { get; set; }
    public Guid? CautelaFornecedorId { get; set; }
    public DateOnly? CautelaDesde { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
    public ICollection<Calibracao> Calibracoes { get; set; } = [];

    public DateOnly? Proxima => UltimaData?.AddMonths(PeriodicidadeMeses);

    public StatusEquipamento Status(DateOnly hoje, int diasAlerta)
    {
        if (Baixado) return StatusEquipamento.Baixado;
        if (EmCalibracao) return StatusEquipamento.EmCalibracao;
        if (UltimaData is null || UltimoResultado == ResultadoCalibracao.Reprovado || Proxima < hoje) return StatusEquipamento.Bloqueado;
        return Proxima <= hoje.AddDays(diasAlerta) ? StatusEquipamento.VenceEmBreve : StatusEquipamento.EmUso;
    }

    public bool UsavelEm(DateOnly hoje) => Status(hoje, 0) is StatusEquipamento.EmUso or StatusEquipamento.VenceEmBreve;

    public void Registrar(Calibracao c)
    {
        if (c.Tipo == TipoControleEquipamento.CalibracaoExterna && string.IsNullOrWhiteSpace(c.Certificado))
            throw new RegraDeNegocioException("Calibração externa: informe o nº do certificado.");
        if (c.Resultado == ResultadoCalibracao.Reprovado && string.IsNullOrWhiteSpace(c.AvaliacaoImpacto))
            throw new RegraDeNegocioException("Reprovado: registre a avaliação de impacto sobre as FVS feitas desde a última aprovação.");
        Calibracoes.Add(c);
        UltimaData = c.Data; UltimoResultado = c.Resultado; UltimoCertificado = c.Certificado; EmCalibracao = false;
    }
}
