using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Suprimentos;

// ═══════════════════════ Equipamentos (patrimônio próprio e locados) e manutenção ═══════════════════════

public enum PropriedadeEquipamento { Propria = 1, Locada = 2, Terceiro = 3 }
public enum Criticidade { A = 1, B = 2, C = 3 }
public enum SituacaoEquipamento { Disponivel = 1, EmUso = 2, EmManutencao = 3, Parado = 4, Devolvido = 5, Baixado = 6 }
public enum TipoMovimentoEquipamento { Chegada = 1, Transferencia = 2, Devolucao = 3, EnvioManutencao = 4, RetornoManutencao = 5, Baixa = 6 }
public enum GatilhoManutencao { Tempo = 1, Uso = 2, Evento = 3 }
public enum TipoOrdemServico { Preventiva = 1, Corretiva = 2, Inspecao = 3 }
public enum StatusOrdemServico { Aberta = 1, EmExecucao = 2, AguardandoPeca = 3, Concluida = 4, Verificada = 5, Cancelada = 6 }

/// <summary>Família de equipamento (elevação, concreto, energia, veículos…). Configurável: campos extras e se usa horímetro/hodômetro.</summary>
public class FamiliaEquipamento : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    public bool UsaHorimetro { get; set; }
    public bool UsaHodometro { get; set; }
    /// <summary>Campos extras separados por ";" (ex.: "Placa;RENAVAM;Chassi").</summary>
    public string? CamposExtras { get; set; }
    public Criticidade CriticidadePadrao { get; set; } = Criticidade.B;
    public bool Ativa { get; set; } = true;

    public IReadOnlyList<string> Campos => (CamposExtras ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Equipamento rastreado um a um (patrimônio próprio, locado ou de terceiro em uso na obra).</summary>
public class Equipamento : EntidadeAuditavel, IHistoricoTemporal
{
    /// <summary>Nº de patrimônio / etiqueta.</summary>
    public string Patrimonio { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public Guid FamiliaId { get; set; }
    /// <summary>Lote do mesmo item locado (ex.: 80 módulos de andaime); próprio = 1.</summary>
    public decimal Quantidade { get; set; } = 1;
    public PropriedadeEquipamento Propriedade { get; set; } = PropriedadeEquipamento.Propria;
    public Guid? FornecedorId { get; set; }
    /// <summary>Locado: contrato de locação (cobrança recorrente).</summary>
    public Guid? ContratoId { get; set; }
    public Guid? ContratoItemId { get; set; }
    public Criticidade Criticidade { get; set; } = Criticidade.B;
    public string? Fabricante { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public int? Ano { get; set; }
    public decimal? ValorAquisicao { get; set; }
    public DateOnly? DataAquisicao { get; set; }
    /// <summary>Aluguel interno por dia apropriado na obra onde o equipamento está (próprio).</summary>
    public decimal? DiariaInterna { get; set; }
    /// <summary>Onde está (almoxarifado central ou da obra).</summary>
    public Guid? AlmoxarifadoId { get; set; }
    public SituacaoEquipamento Situacao { get; set; } = SituacaoEquipamento.Disponivel;
    public decimal Horimetro { get; set; }
    public decimal Hodometro { get; set; }
    /// <summary>Valores dos campos extras da família ("Placa=ABC1D23;RENAVAM=…").</summary>
    public string? CamposExtras { get; set; }
    /// <summary>Locado: data combinada para devolver.</summary>
    public DateOnly? DevolverAte { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
    public string? Observacao { get; set; }
    public ICollection<EquipamentoDocumento> Documentos { get; set; } = [];

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Patrimonio)) e.Add("Informe o nº de patrimônio / identificação.");
        if (string.IsNullOrWhiteSpace(Nome)) e.Add("Informe o nome do equipamento.");
        if (FamiliaId == Guid.Empty) e.Add("Informe a família.");
        if (Propriedade == PropriedadeEquipamento.Locada && ContratoId is null && FornecedorId is null) e.Add("Equipamento locado: informe o contrato ou a locadora.");
        return e;
    }

    public string Campo(string nome) =>
        (CamposExtras ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=', 2)).FirstOrDefault(p => p[0].Trim() == nome) is { Length: 2 } kv ? kv[1] : "";
}

public class EquipamentoDocumento : Entidade
{
    public Guid EquipamentoId { get; set; }
    /// <summary>Ex.: Laudo NR-12, ART de montagem, seguro.</summary>
    public string Nome { get; set; } = string.Empty;
    public DateOnly? Vencimento { get; set; }
    public bool Bloqueia { get; set; } = true;
}

/// <summary>Movimentação do equipamento (chegada, transferência entre obras, devolução, manutenção) com checklist.</summary>
public class MovimentoEquipamento : EntidadeAuditavel
{
    public Guid EquipamentoId { get; set; }
    public DateOnly Data { get; set; }
    public TipoMovimentoEquipamento Tipo { get; set; }
    public Guid? DeAlmoxarifadoId { get; set; }
    public Guid? ParaAlmoxarifadoId { get; set; }
    public string? Checklist { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Plano de manutenção preventiva: por tempo (dias), por uso (horas/km) ou por evento (antes de mobilizar).</summary>
public class PlanoManutencao : EntidadeAuditavel
{
    public Guid EquipamentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public GatilhoManutencao Gatilho { get; set; } = GatilhoManutencao.Tempo;
    public int? IntervaloDias { get; set; }
    public decimal? IntervaloUso { get; set; }
    public DateOnly? UltimaData { get; set; }
    public decimal? UltimaLeitura { get; set; }
    /// <summary>Quem executa: mecânico próprio, técnico de segurança, contrato…</summary>
    public string Executante { get; set; } = string.Empty;
    public Guid? ContratoId { get; set; }
    /// <summary>Itens do checklist, um por linha.</summary>
    public string? Checklist { get; set; }
    public int DiasAntecedencia { get; set; } = 7;
    public bool Ativo { get; set; } = true;

    public DateOnly? ProximaData => Gatilho == GatilhoManutencao.Tempo && IntervaloDias is > 0 ? (UltimaData?.AddDays(IntervaloDias.Value)) : null;
    public decimal? ProximaLeitura => Gatilho == GatilhoManutencao.Uso && IntervaloUso is > 0 ? (UltimaLeitura ?? 0) + IntervaloUso : null;
    public IReadOnlyList<string> ItensChecklist => (Checklist ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool Vencido(DateOnly hoje, decimal leituraAtual) =>
        Ativo && (ProximaData is { } d && d < hoje || ProximaLeitura is { } l && leituraAtual >= l);
    public bool PertoDeVencer(DateOnly hoje, decimal leituraAtual) =>
        Ativo && !Vencido(hoje, leituraAtual) && (ProximaData is { } d && d <= hoje.AddDays(DiasAntecedencia) || ProximaLeitura is { } l && IntervaloUso is > 0 && leituraAtual >= l - IntervaloUso.Value * 0.1m);
}

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

/// <summary>Regras de liberação do equipamento e indicadores de manutenção (funções puras).</summary>
public static class RegrasManutencao
{
    /// <summary>Motivos que bloqueiam o uso (vazio = liberado).</summary>
    public static List<string> Bloqueios(Equipamento e, IEnumerable<PlanoManutencao> planos, IEnumerable<OrdemServico> ordens, DateOnly hoje)
    {
        var b = new List<string>();
        if (e.Situacao is SituacaoEquipamento.Baixado or SituacaoEquipamento.Devolvido) b.Add(e.Situacao == SituacaoEquipamento.Baixado ? "baixado" : "devolvido à locadora");
        if (e.Situacao == SituacaoEquipamento.EmManutencao) b.Add("em manutenção");
        foreach (var d in e.Documentos.Where(d => d.Bloqueia && d.Vencimento is { } v && v < hoje)) b.Add($"{d.Nome} vencido em {d.Vencimento:dd/MM/yyyy}");
        foreach (var p in planos.Where(p => p.EquipamentoId == e.Id && p.Vencido(hoje, e.Horimetro + e.Hodometro)))
            b.Add($"preventiva vencida: {p.Nome}");
        foreach (var o in ordens.Where(o => o.EquipamentoId == e.Id && o.EmAberto && o.Tipo == TipoOrdemServico.Corretiva && (e.Criticidade == Criticidade.A || o.ParadoDesde is not null)))
            b.Add($"corretiva aberta (OS {o.Numero})");
        return b;
    }

    /// <summary>Disponibilidade no período = 1 − horas paradas / horas do período.</summary>
    public static decimal Disponibilidade(IEnumerable<OrdemServico> ordens, DateTimeOffset inicio, DateTimeOffset fim, int equipamentos)
    {
        if (equipamentos <= 0 || fim <= inicio) return 1;
        var total = (fim - inicio).TotalHours * equipamentos;
        var parado = ordens.Where(o => o.ParadoDesde is not null).Sum(o =>
        {
            var a = o.ParadoDesde!.Value < inicio ? inicio : o.ParadoDesde.Value;
            var b = (o.ParadoAte ?? fim) > fim ? fim : (o.ParadoAte ?? fim);
            return b > a ? (b - a).TotalHours : 0;
        });
        return Math.Round((decimal)Math.Max(0, 1 - parado / total), 4);
    }

    /// <summary>MTTR (horas para reparar) e MTBF (horas entre falhas) das corretivas no período.</summary>
    public static (decimal Mttr, decimal Mtbf) Indicadores(IEnumerable<OrdemServico> corretivas, double horasOperacao)
    {
        var lista = corretivas.Where(o => o.Tipo == TipoOrdemServico.Corretiva && o.ParadoDesde is not null && o.ParadoAte is not null).ToList();
        if (lista.Count == 0) return (0, (decimal)Math.Round(horasOperacao, 1));
        var mttr = lista.Average(o => (o.ParadoAte!.Value - o.ParadoDesde!.Value).TotalHours);
        return ((decimal)Math.Round(mttr, 1), (decimal)Math.Round(horasOperacao / lista.Count, 1));
    }
}
