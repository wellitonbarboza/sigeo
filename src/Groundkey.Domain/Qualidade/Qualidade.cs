using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;

namespace Groundkey.Domain.Qualidade;

// ═══════════════════════ Q01/Q02/Q12 — Modelos de inspeção ═══════════════════════

/// <summary>Q01 · Modelo de FVS (serviço), FVM (material) ou checklist SST. Segue a revisão do documento (ITS / especificação).</summary>
public class ModeloInspecao : EntidadeAuditavel, IHistoricoTemporal
{
    public TipoModelo Tipo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public Guid? ServicoControladoId { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public string? NormaReferencia { get; set; }
    public string? Amostragem { get; set; }
    public bool QuarentenaAteEnsaio { get; set; }
    public bool RastrearAplicacao { get; set; }
    public Guid? DocumentoId { get; set; }
    public Guid? RevisaoDocumentoId { get; set; }
    public int Revisao { get; set; }
    public string? MomentoInspecao { get; set; }
    /// <summary>FVS: pré-requisitos para a execução do serviço. FVM: orientações de armazenamento. Mostrado ao inspetor.</summary>
    public string? Orientacoes { get; set; }
    public string? NivelLocal { get; set; }
    public bool LiberaMedicao { get; set; } = true;
    public Guid? ServicoSeguinteId { get; set; }
    public SituacaoModelo Situacao { get; set; } = SituacaoModelo.Rascunho;
    public ICollection<ItemVerificacao> Itens { get; set; } = [];
    public ICollection<EnsaioModelo> Ensaios { get; set; } = [];

    public string Rotulo => $"R{Revisao:00}";

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Codigo)) e.Add("Informe o código do modelo.");
        if (Tipo == TipoModelo.FVS && ServicoControladoId is null) e.Add("FVS: informe o serviço controlado (ITS).");
        if (Tipo == TipoModelo.FVM && MaterialControladoId is null) e.Add("FVM: informe o material controlado.");
        if (Tipo == TipoModelo.FVM && string.IsNullOrWhiteSpace(NormaReferencia)) e.Add("FVM: informe a norma / especificação.");
        if (Tipo == TipoModelo.FVM && string.IsNullOrWhiteSpace(Amostragem)) e.Add("FVM: informe a amostragem.");
        if (Itens.Count == 0) e.Add("Inclua ao menos um item de verificação.");
        foreach (var i in Itens)
        {
            if (string.IsNullOrWhiteSpace(i.Descricao)) e.Add($"Item {i.Ordem}: descrição obrigatória.");
            if (string.IsNullOrWhiteSpace(i.Criterio)) e.Add($"Item {i.Ordem}: critério de aceitação obrigatório.");
            if (i.ValorMin is { } mi && i.ValorMax is { } ma && ma < mi) e.Add($"Item {i.Ordem}: valor máximo menor que o mínimo.");
        }
        foreach (var x in Ensaios.Where(x => x.Momento == MomentoEnsaio.Posterior && string.IsNullOrWhiteSpace(x.PrazoDias)))
            e.Add($"Ensaio '{x.Ensaio}': informe a idade / prazo em dias.");
        return e;
    }

    /// <summary>Nova revisão do documento-base: modelo vigente entra em revisão (FVS novas só com o modelo revisado).</summary>
    public void DocumentoRevisado()
    {
        if (Situacao == SituacaoModelo.Vigente) Situacao = SituacaoModelo.EmRevisao;
    }

    public void Publicar(Guid? revisaoDocumentoId)
    {
        ValidacaoFalha.Se(Validar());
        if (Situacao is SituacaoModelo.EmRevisao) Revisao++;
        RevisaoDocumentoId = revisaoDocumentoId ?? RevisaoDocumentoId;
        Situacao = SituacaoModelo.Vigente;
    }
}

/// <summary>Q02 · Item de verificação do modelo.</summary>
public class ItemVerificacao : Entidade
{
    public Guid ModeloInspecaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
    public bool ExigeEquipamento { get; set; }
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public FotoItem Foto { get; set; } = FotoItem.SoEmNaoConformidade;
    public bool Bloqueia { get; set; } = true;
    public string? Referencia { get; set; } // SST: NR aplicável

    /// <summary>Com mínimo/máximo definidos, o valor medido decide sozinho.</summary>
    public ResultadoItem? AvaliarValor(decimal? medido)
    {
        if (medido is null || (ValorMin is null && ValorMax is null)) return null;
        return (ValorMin is null || medido >= ValorMin) && (ValorMax is null || medido <= ValorMax) ? ResultadoItem.Conforme : ResultadoItem.NaoConforme;
    }
}

/// <summary>Q12 · Ensaio de recebimento previsto no modelo de FVM.</summary>
public class EnsaioModelo : Entidade
{
    public Guid ModeloInspecaoId { get; set; }
    public string Ensaio { get; set; } = string.Empty;
    public string? Norma { get; set; }
    public MomentoEnsaio Momento { get; set; } = MomentoEnsaio.NaChegada;
    public string? PrazoDias { get; set; }
    public string Frequencia { get; set; } = string.Empty;
    public ExecutanteEnsaio Executante { get; set; } = ExecutanteEnsaio.Obra;
    public string Criterio { get; set; } = string.Empty;
    public AcaoReprovacaoEnsaio AcaoReprovacao { get; set; } = AcaoReprovacaoEnsaio.AbrirRnc;

    /// <summary>Maior idade informada (ex.: "7 e 28" → 28).</summary>
    public int MaiorPrazo() =>
        (PrazoDias ?? "").Split([' ', ',', ';', '/', 'e'], StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => int.TryParse(x, out var n) ? n : 0).DefaultIfEmpty(0).Max();
}

/// <summary>Erro de validação de domínio (lista de mensagens legíveis).</summary>
public sealed class ValidacaoFalha(IReadOnlyList<string> erros) : RegraDeNegocioException(string.Join(" | ", erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;
    public static void Se(IReadOnlyList<string> erros) { if (erros.Count > 0) throw new ValidacaoFalha(erros); }
}

// ═══════════════════════ Q04/Q05 — FVS ═══════════════════════

/// <summary>Q04 · Ficha de verificação de serviço: uma por local × serviço (e por reinspeção).</summary>
public class Fvs : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public Guid LocalId { get; set; }
    public Local? Local { get; set; }
    public Guid ModeloInspecaoId { get; set; }
    public ModeloInspecao? Modelo { get; set; }
    public int RevisaoModelo { get; set; }
    public Guid? ServicoControladoId { get; set; }
    public Guid FornecedorId { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid InspetorId { get; set; }
    public string InspetorNome { get; set; } = string.Empty;
    public DateTimeOffset DataInspecao { get; set; }
    public Guid? EquipamentoId { get; set; }
    public string? Disposicao { get; set; }
    public DateOnly? PrazoCorrecao { get; set; }
    public Guid? RncId { get; set; }
    public Guid? FvsOrigemId { get; set; }
    public DateOnly? DataReinspecao { get; set; }
    public StatusFvs Status { get; set; } = StatusFvs.EmInspecao;
    public Guid? LiberadoPorId { get; set; }
    public string? LiberadoPorNome { get; set; }
    public DateTimeOffset? DataLiberacao { get; set; }
    public OrigemRegistro Origem { get; set; } = OrigemRegistro.Web;
    public ICollection<FvsItem> Itens { get; set; } = [];

    public int Conformes => Itens.Count(i => i.ResultadoFinal == ResultadoItem.Conforme);
    public int Avaliados => Itens.Count(i => i.ResultadoFinal != ResultadoItem.NaoAvaliado);
    public string Resumo => $"{Conformes}/{Itens.Count} conformes";
    public bool Liberada => Status == StatusFvs.Liberada;
    public bool Aprovada1aInspecao => FvsOrigemId is null && Status is StatusFvs.Aprovada or StatusFvs.Liberada && Itens.All(i => i.ResultadoReinspecao is null);

    /// <summary>Copia os itens do modelo vigente (descrição e critério congelados na revisão usada).</summary>
    public static Fvs Abrir(ModeloInspecao m, Local local, Guid fornecedorId, Guid inspetorId, string inspetorNome, DateTimeOffset agora, string numero)
    {
        if (m.Tipo != TipoModelo.FVS) throw new RegraDeNegocioException("Modelo não é de FVS.");
        if (m.Situacao != SituacaoModelo.Vigente) throw new RegraDeNegocioException($"Modelo {m.Codigo} não está vigente ({m.Situacao}).");
        if (!string.IsNullOrWhiteSpace(m.NivelLocal) && !string.Equals(m.NivelLocal, local.Tipo, StringComparison.OrdinalIgnoreCase))
            throw new RegraDeNegocioException($"Este modelo é aberto no nível '{m.NivelLocal}'; o local escolhido é '{local.Tipo}'.");
        var f = new Fvs
        {
            ObraId = local.ObraId, LocalId = local.Id, ModeloInspecaoId = m.Id, RevisaoModelo = m.Revisao, ServicoControladoId = m.ServicoControladoId,
            FornecedorId = fornecedorId, InspetorId = inspetorId, InspetorNome = inspetorNome, DataInspecao = agora, Numero = numero
        };
        foreach (var i in m.Itens.OrderBy(i => i.Ordem))
            f.Itens.Add(new FvsItem
            {
                FvsId = f.Id, ItemVerificacaoId = i.Id, Ordem = i.Ordem, Descricao = i.Descricao, Metodo = i.Metodo, Criterio = i.Criterio,
                ValorMin = i.ValorMin, ValorMax = i.ValorMax, Foto = i.Foto, Bloqueia = i.Bloqueia, ExigeEquipamento = i.ExigeEquipamento
            });
        return f;
    }

    /// <summary>Conclui a inspeção: qualquer não conforme reprova; reprovação exige disposição e prazo.</summary>
    public IReadOnlyList<string> Concluir(bool equipamentoValido, IReadOnlyDictionary<Guid, int>? fotosPorItem = null)
    {
        var e = new List<string>();
        if (Status is not (StatusFvs.EmInspecao or StatusFvs.Reinspecao)) e.Add("FVS já concluída.");
        foreach (var i in Itens) i.AplicarValor();
        if (Itens.Any(i => i.Resultado == ResultadoItem.NaoAvaliado)) e.Add("Avalie todos os itens (Conforme, Não conforme ou N/A).");
        foreach (var i in Itens.Where(i => i.Resultado is ResultadoItem.NaoConforme or ResultadoItem.NaoAplica && string.IsNullOrWhiteSpace(i.Observacao)))
            e.Add($"Item {i.Ordem}: observação obrigatória para Não conforme / N/A.");
        if (fotosPorItem is not null)
            foreach (var i in Itens.Where(i => i.Foto == FotoItem.Obrigatoria || (i.Foto == FotoItem.SoEmNaoConformidade && i.Resultado == ResultadoItem.NaoConforme)))
                if (fotosPorItem.GetValueOrDefault(i.Id) == 0) e.Add($"Item {i.Ordem}: foto obrigatória.");
        if (Itens.Any(i => i.ExigeEquipamento) && EquipamentoId is null) e.Add("Informe o equipamento de medição usado.");
        if (EquipamentoId is not null && !equipamentoValido) e.Add("Equipamento com calibração vencida ou reprovada não pode ser usado.");
        var reprovada = Itens.Any(i => i.Resultado == ResultadoItem.NaoConforme);
        if (reprovada && string.IsNullOrWhiteSpace(Disposicao)) e.Add("FVS reprovada: informe a disposição (retrabalho, reparo…).");
        if (reprovada && PrazoCorrecao is null) e.Add("FVS reprovada: informe o prazo da correção.");
        if (e.Count > 0) return e;
        Status = reprovada ? StatusFvs.Reprovada : StatusFvs.Aprovada;
        return e;
    }

    /// <summary>Reinspeção: reavalia só os itens não conformes na própria ficha (histórico preservado no item).</summary>
    public IReadOnlyList<string> Reinspecionar(DateOnly data, IReadOnlyDictionary<Guid, ResultadoItem> resultados)
    {
        var e = new List<string>();
        if (Status != StatusFvs.Reprovada) e.Add("Só FVS reprovada pode ser reinspecionada.");
        var nc = Itens.Where(i => i.Resultado == ResultadoItem.NaoConforme).ToList();
        foreach (var i in nc)
        {
            if (!resultados.TryGetValue(i.Id, out var r) || r is not (ResultadoItem.Conforme or ResultadoItem.NaoConforme)) e.Add($"Item {i.Ordem}: informe o resultado da reinspeção.");
            else i.ResultadoReinspecao = r;
        }
        if (e.Count > 0) return e;
        DataReinspecao = data;
        Status = nc.All(i => i.ResultadoReinspecao == ResultadoItem.Conforme) ? StatusFvs.Aprovada : StatusFvs.Reprovada;
        return e;
    }

    /// <summary>Liberação pelo engenheiro: só FVS aprovada; libera medição e etapa seguinte.</summary>
    public void Liberar(Guid usuarioId, string nome, DateTimeOffset agora)
    {
        if (Status != StatusFvs.Aprovada) throw new RegraDeNegocioException("Só FVS aprovada pode ser liberada.");
        Status = StatusFvs.Liberada; LiberadoPorId = usuarioId; LiberadoPorNome = nome; DataLiberacao = agora;
    }

    /// <summary>Reprovada com item que bloqueia → etapa seguinte do mesmo local bloqueada.</summary>
    public bool BloqueiaEtapaSeguinte => Status == StatusFvs.Reprovada && Itens.Any(i => i.Bloqueia && i.ResultadoFinal == ResultadoItem.NaoConforme);
}

/// <summary>Q05 · Resultado de cada item da FVS.</summary>
public class FvsItem : Entidade
{
    public Guid FvsId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Metodo { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public FotoItem Foto { get; set; }
    public bool Bloqueia { get; set; }
    public bool ExigeEquipamento { get; set; }
    public ResultadoItem Resultado { get; set; }
    public decimal? ValorMedido { get; set; }
    public string? Observacao { get; set; }
    public ResultadoItem? ResultadoReinspecao { get; set; }

    public ResultadoItem ResultadoFinal => ResultadoReinspecao ?? Resultado;

    public void AplicarValor()
    {
        if (ValorMedido is null || (ValorMin is null && ValorMax is null)) return;
        Resultado = (ValorMin is null || ValorMedido >= ValorMin) && (ValorMax is null || ValorMedido <= ValorMax) ? ResultadoItem.Conforme : ResultadoItem.NaoConforme;
    }
}

// ═══════════════════════ Q06/Q07 — RNC e ação corretiva ═══════════════════════

/// <summary>Q06 · Registro de não conformidade.</summary>
public class Rnc : EntidadeAuditavel, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public OrigemRnc Origem { get; set; }
    public string? Referencia { get; set; }
    public Guid? ReferenciaId { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? LocalId { get; set; }
    public string? LocaisAfetados { get; set; }
    public Guid? FornecedorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public ClassificacaoRnc Classificacao { get; set; } = ClassificacaoRnc.Servico;
    public Gravidade Gravidade { get; set; } = Gravidade.Media;
    public string? Disposicao { get; set; }
    public string? Concessao { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly Prazo { get; set; }
    public bool ExigeAcao { get; set; }
    public Guid? AcaoCorretivaId { get; set; }
    public StatusRnc Status { get; set; } = StatusRnc.Aberta;
    public string? EncerradaPor { get; set; }
    public DateTimeOffset? EncerradaEm { get; set; }

    public bool Vencida(DateOnly hoje) => Status is StatusRnc.Aberta or StatusRnc.EmTratamento && Prazo < hoje;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a não conformidade.");
        if (string.IsNullOrWhiteSpace(Disposicao)) e.Add("Informe a disposição (tratamento imediato).");
        if (Disposicao?.Contains("concess", StringComparison.OrdinalIgnoreCase) == true && string.IsNullOrWhiteSpace(Concessao))
            e.Add("Aceite sob concessão: registre a aprovação do projetista / Diretoria.");
        if (ResponsavelId is null && string.IsNullOrWhiteSpace(ResponsavelNome)) e.Add("Informe o responsável.");
        if (Gravidade >= Gravidade.Alta && !ExigeAcao) e.Add("Gravidade Alta exige ação corretiva.");
        return e;
    }

    /// <summary>Gravidade alta ou reincidência (mesmo serviço/prestador no período) obrigam ação corretiva.</summary>
    public void AvaliarExigencia(int reincidencias, int limiteReincidencia)
    {
        if (Gravidade >= Gravidade.Alta || reincidencias >= limiteReincidencia) ExigeAcao = true;
    }

    public void Encerrar(string por, DateTimeOffset agora, bool acaoEncerrada)
    {
        if (Status is StatusRnc.Encerrada or StatusRnc.Cancelada) throw new RegraDeNegocioException("RNC já encerrada.");
        if (ExigeAcao && AcaoCorretivaId is null) throw new RegraDeNegocioException("Esta RNC exige ação corretiva: abra a AC antes de encerrar.");
        if (ExigeAcao && !acaoEncerrada) { Status = StatusRnc.AguardandoEficacia; return; }
        Status = StatusRnc.Encerrada; EncerradaPor = por; EncerradaEm = agora;
    }
}

/// <summary>Q07 · Ação corretiva: causa raiz, plano e eficácia. Reúne uma ou mais RNC (e investigação de acidentes).</summary>
public class AcaoCorretiva : EntidadeAuditavel, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public string? RncsOrigem { get; set; }
    public Guid? OcorrenciaId { get; set; }
    public string Problema { get; set; } = string.Empty;
    public MetodoAnalise MetodoAnalise { get; set; } = MetodoAnalise.CincoPorques;
    public string? AnaliseCausa { get; set; }
    public string? CausaRaiz { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly Prazo { get; set; }
    public DateOnly? DataEficacia { get; set; }
    public string? CriterioEficacia { get; set; }
    public Eficacia ResultadoEficacia { get; set; }
    public bool AtualizaSgq { get; set; }
    public StatusAcaoCorretiva Status { get; set; } = StatusAcaoCorretiva.EmAnalise;
    public ICollection<AcaoCorretivaItem> Itens { get; set; } = [];

    public IReadOnlyList<string> ValidarPlano()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Problema)) e.Add("Descreva o problema.");
        if (string.IsNullOrWhiteSpace(AnaliseCausa)) e.Add("Registre a análise de causa.");
        if (string.IsNullOrWhiteSpace(CausaRaiz)) e.Add("Informe a causa raiz.");
        if (Itens.Count == 0) e.Add("Inclua ao menos uma ação no plano.");
        if (DataEficacia is null) e.Add("Informe a data prevista da verificação de eficácia.");
        if (string.IsNullOrWhiteSpace(CriterioEficacia)) e.Add("Informe o critério de eficácia.");
        return e;
    }

    /// <summary>Avança o status conforme o plano; eficácia registrada encerra (não eficaz → volta à análise).</summary>
    public void Atualizar()
    {
        if (Status == StatusAcaoCorretiva.Encerrada) return;
        if (ResultadoEficacia == Eficacia.NaoEficaz)
        {
            Status = StatusAcaoCorretiva.EmAnalise; ResultadoEficacia = Eficacia.NaoVerificada;
            foreach (var i in Itens) i.Status = StatusItemPlano.Pendente;
            return;
        }
        if (ResultadoEficacia == Eficacia.Eficaz)
        {
            if (Itens.Any(i => i.Status == StatusItemPlano.Pendente)) throw new RegraDeNegocioException("Conclua as ações do plano antes de registrar a eficácia.");
            Status = StatusAcaoCorretiva.Encerrada; return;
        }
        if (Itens.Count > 0 && ValidarPlano().Count == 0)
            Status = Itens.All(i => i.Status != StatusItemPlano.Pendente) ? StatusAcaoCorretiva.AguardandoEficacia : StatusAcaoCorretiva.EmExecucao;
    }
}

public class AcaoCorretivaItem : Entidade
{
    public Guid AcaoCorretivaId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateOnly Prazo { get; set; }
    public StatusItemPlano Status { get; set; } = StatusItemPlano.Pendente;
    public Guid? DocumentoId { get; set; }
    public string? Resultado { get; set; }
}

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

public enum DonoFerramenta { Propria = 1, Terceiro = 2 }

/// <summary>Tipo de equipamento de medição (trena, nível, prumo…): padrões configuráveis por construtora.</summary>
public class TipoEquipamentoMedicao : EntidadeAuditavel
{
    public string Nome { get; set; } = string.Empty;
    /// <summary>Prefixo do código (TR, NV, PR…).</summary>
    public string Prefixo { get; set; } = string.Empty;
    public int PeriodicidadeMeses { get; set; } = 6;
    public string Criterio { get; set; } = string.Empty;
    public string? Metodo { get; set; }
    public TipoControleEquipamento TipoControle { get; set; } = TipoControleEquipamento.VerificacaoInterna;
    public bool Ativo { get; set; } = true;
}

/// <summary>Cautela: entrega e devolução da ferramenta de medição (para colaborador próprio, empreiteira ou setor).</summary>
public class CautelaFerramenta : EntidadeAuditavel
{
    public Guid EquipamentoMedicaoId { get; set; }
    public DateOnly Data { get; set; }
    public string Para { get; set; } = string.Empty;
    public Guid? ColaboradorId { get; set; }
    public Guid? FornecedorId { get; set; }
    public DateOnly? DevolvidaEm { get; set; }
    public string? EntreguePor { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Q09 · Calibração (externa, RBC) ou verificação interna.</summary>
public class Calibracao : Entidade
{
    public Guid EquipamentoMedicaoId { get; set; }
    public TipoControleEquipamento Tipo { get; set; }
    public DateOnly Data { get; set; }
    public string Executante { get; set; } = string.Empty;
    public Guid? LaboratorioId { get; set; }
    public string? Certificado { get; set; }
    public string Erro { get; set; } = string.Empty;
    public ResultadoCalibracao Resultado { get; set; } = ResultadoCalibracao.Aprovado;
    public string? AvaliacaoImpacto { get; set; }
    public string? RegistradoPor { get; set; }
}

// ═══════════════════════ Q10/Q11 — Auditoria interna ═══════════════════════

public class AuditoriaInterna : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public string Escopo { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public string Auditores { get; set; } = string.Empty;
    public string? Independencia { get; set; }
    public string? ResumoAnaliseCritica { get; set; }
    public StatusAuditoria Status { get; set; } = StatusAuditoria.Planejada;
    public ICollection<Constatacao> Constatacoes { get; set; } = [];

    public int Ncs => Constatacoes.Count(c => c.Tipo == TipoConstatacao.NaoConformidade);
    public int Oms => Constatacoes.Count(c => c.Tipo == TipoConstatacao.OportunidadeMelhoria);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Escopo)) e.Add("Informe o escopo.");
        if (string.IsNullOrWhiteSpace(Criterio)) e.Add("Informe o critério.");
        if (string.IsNullOrWhiteSpace(Auditores)) e.Add("Informe o(s) auditor(es).");
        if (Fim < Inicio) e.Add("Fim anterior ao início.");
        if (string.IsNullOrWhiteSpace(Independencia)) e.Add("Registre a independência (auditor não audita o próprio trabalho).");
        return e;
    }

    public void EmitirRelatorio()
    {
        if (Constatacoes.Count == 0) throw new RegraDeNegocioException("Inclua o checklist.");
        if (Constatacoes.Any(c => c.Tipo == TipoConstatacao.NaoAvaliado)) throw new RegraDeNegocioException("Registre o resultado de todos os itens do checklist.");
        if (Constatacoes.Any(c => c.Tipo != TipoConstatacao.Conforme && string.IsNullOrWhiteSpace(c.Evidencia)))
            throw new RegraDeNegocioException("Constatação diferente de Conforme exige evidência.");
        Status = StatusAuditoria.RelatorioEmitido;
    }
}

public class Constatacao : Entidade
{
    public Guid AuditoriaInternaId { get; set; }
    public int Ordem { get; set; }
    public string Requisito { get; set; } = string.Empty;
    public string Pergunta { get; set; } = string.Empty;
    public string? Evidencia { get; set; }
    public TipoConstatacao Tipo { get; set; }
    public Guid? RncId { get; set; }
}

// ═══════════════════════ Q13/Q14/Q15 — FVM detalhada, ensaios e rastreabilidade ═══════════════════════

/// <summary>Q13 · Resultado de cada item do modelo de FVM no recebimento.</summary>
public class FvmItem : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Guid? RecebimentoItemId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public decimal? ValorMin { get; set; }
    public decimal? ValorMax { get; set; }
    public bool Bloqueia { get; set; }
    public string? ValorMedido { get; set; }
    public ResultadoItem Resultado { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Q14 · Ensaio executado num lote recebido.</summary>
public class EnsaioRecebimento : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid RecebimentoId { get; set; }
    public Guid EnsaioModeloId { get; set; }
    public string Ensaio { get; set; } = string.Empty;
    public string Criterio { get; set; } = string.Empty;
    public AcaoReprovacaoEnsaio AcaoReprovacao { get; set; }
    public string Amostra { get; set; } = string.Empty;
    public DateOnly DataColeta { get; set; }
    public DateOnly? DataPrevista { get; set; }
    public Guid? LaboratorioId { get; set; }
    public string? Resultado { get; set; }
    public SituacaoEnsaio Situacao { get; set; } = SituacaoEnsaio.Aguardando;
    public Guid? RncId { get; set; }

    public bool Atrasado(DateOnly hoje) => Situacao == SituacaoEnsaio.Aguardando && DataPrevista < hoje;
}

/// <summary>Q15 · Onde o lote de material controlado foi aplicado (rastreabilidade).</summary>
public class AplicacaoLote : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Guid LocalId { get; set; }
    public decimal Quantidade { get; set; }
    public DateOnly Data { get; set; }
    public Guid? FvsId { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;
    public DateTimeOffset RegistradoEm { get; set; }
}

// ═══════════════════════ Regras transversais ═══════════════════════

public static class RegrasQualidadeObra
{
    /// <summary>Lote com quarentena só é aplicado depois dos ensaios aprovados; soma aplicada ≤ recebida.</summary>
    public static IReadOnlyList<string> PodeAplicar(StatusLote status, bool quarentena, IEnumerable<EnsaioRecebimento> ensaios,
        decimal recebido, decimal jaAplicado, decimal quantidade)
    {
        var e = new List<string>();
        if (quantidade <= 0) e.Add("Quantidade aplicada deve ser positiva.");
        if (status is StatusLote.Devolvido) e.Add("Lote devolvido não pode ser aplicado.");
        if (quarentena && status == StatusLote.EmQuarentena && ensaios.Any(x => x.Situacao != SituacaoEnsaio.Aprovado))
            e.Add("Lote em quarentena: aguarde a aprovação dos ensaios.");
        if (ensaios.Any(x => x.Situacao == SituacaoEnsaio.Reprovado)) e.Add("Lote com ensaio reprovado: trate a RNC antes de aplicar.");
        if (jaAplicado + quantidade > recebido) e.Add($"Soma aplicada ({jaAplicado + quantidade:0.###}) passa da quantidade recebida ({recebido:0.###}).");
        return e;
    }

    /// <summary>Status do lote a partir dos ensaios (quarentena só libera com todos aprovados).</summary>
    public static StatusLote StatusDoLote(bool fvmAprovada, bool devolvido, bool concessao, bool quarentena, IReadOnlyCollection<EnsaioRecebimento> ensaios)
    {
        if (devolvido) return StatusLote.Devolvido;
        if (concessao) return StatusLote.AceitoSobConcessao;
        if (!fvmAprovada) return StatusLote.EmVerificacao;
        if (quarentena && (ensaios.Count == 0 || ensaios.Any(x => x.Situacao != SituacaoEnsaio.Aprovado))) return StatusLote.EmQuarentena;
        return StatusLote.Liberado;
    }

    /// <summary>Etapa bloqueada: FVS do serviço anterior (cujo modelo aponta este serviço como seguinte) reprovada no mesmo local.</summary>
    public static string? EtapaBloqueada(Guid localId, Guid servicoId, IEnumerable<(Fvs Fvs, Guid? ServicoSeguinteId)> fvsDoLocal)
    {
        var bloqueio = fvsDoLocal.FirstOrDefault(x => x.Fvs.LocalId == localId && x.ServicoSeguinteId == servicoId && x.Fvs.BloqueiaEtapaSeguinte);
        return bloqueio.Fvs is null ? null : $"Etapa bloqueada pela {bloqueio.Fvs.Numero} reprovada neste local — aguarde a reinspeção.";
    }
}
