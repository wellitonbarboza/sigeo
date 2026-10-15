using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Qualidade;

namespace Groundkey.Domain.Sst;

// ═══════════════════════ S02/S03/S03A — Funções e exigências ═══════════════════════

/// <summary>S02 · Função da obra (base da matriz de exigências).</summary>
public class FuncaoObra : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public string? Cbo { get; set; }
    public string? Riscos { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<FuncaoExigencia> Exigencias { get; set; } = [];
}

/// <summary>S03 · Exigência (documento, ASO, treinamento, EPI). Valores de referência — validar com SST/PGR/PCMSO.</summary>
public class Exigencia : EntidadeAuditavel
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoExigencia Tipo { get; set; }
    public Guid? TreinamentoId { get; set; }
    public string? CargaInicial { get; set; }
    public int? ValidadeMeses { get; set; }
    public EfeitoVencimento EfeitoVencimento { get; set; } = EfeitoVencimento.BloqueiaPessoa;
    public AtividadeRisco Atividades { get; set; }
    public string Fonte { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

/// <summary>S03A · Matriz função × exigência (O = obrigatório, C = conforme a atividade).</summary>
public class FuncaoExigencia : Entidade
{
    public Guid FuncaoObraId { get; set; }
    public Guid ExigenciaId { get; set; }
    public Exigencia? Exigencia { get; set; }
    public Obrigatoriedade Obrigatoriedade { get; set; } = Obrigatoriedade.Obrigatorio;
}

// ═══════════════════════ S01/S04 — Colaboradores e documentos ═══════════════════════

/// <summary>S01 · Pessoa que trabalha na obra (própria ou terceira). CPF só como hash + máscara (LGPD).</summary>
public class Colaborador : EntidadeDaObra, IHistoricoTemporal
{
    public string Nome { get; set; } = string.Empty;
    public string CpfHash { get; set; } = string.Empty;
    public string CpfMascarado { get; set; } = string.Empty;
    public VinculoColaborador Vinculo { get; set; } = VinculoColaborador.Terceiro;
    public Guid? FornecedorId { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid FuncaoObraId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataSaida { get; set; }
    public string? Telefone { get; set; }
    public bool ConsentimentoTelefone { get; set; }
    public SituacaoColaborador Situacao { get; set; } = SituacaoColaborador.Pendente;
    public AtividadeRisco Restricoes { get; set; }
    public string? Pendencias { get; set; }
    public string QrToken { get; set; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();

    public static string Mascarar(string cpfDigitos) =>
        cpfDigitos.Length == 11 ? $"***.{cpfDigitos.Substring(3, 3)}.***-{cpfDigitos[^2..]}" : "***";
}

/// <summary>S04 · Documento/ASO da pessoa. ASO guarda só conclusão, aptidões e validade — nunca diagnóstico.</summary>
public class DocumentoColaborador : EntidadeAuditavel, IHistoricoTemporal
{
    public Guid ColaboradorId { get; set; }
    public Guid ExigenciaId { get; set; }
    public DateOnly DataEmissao { get; set; }
    public DateOnly? Validade { get; set; }
    public ConclusaoAso? ConclusaoAso { get; set; }
    public AtividadeRisco AptoAtividades { get; set; }
    public string? ArquivoBlob { get; set; }
    public string? ArquivoNome { get; set; }
    public bool ArquivoRestrito { get; set; }
    public OrigemDocColaborador Origem { get; set; } = OrigemDocColaborador.Sst;
    public ValidacaoDoc Validacao { get; set; } = ValidacaoDoc.Aguardando;
    public string? MotivoRecusa { get; set; }
    public string? ValidadoPor { get; set; }
    public DateTimeOffset? ValidadoEm { get; set; }

    public IReadOnlyList<string> Validar(Exigencia ex)
    {
        var e = new List<string>();
        if (ex.ValidadeMeses is not null && Validade is null) e.Add("Informe a validade.");
        if (Validade is { } v && v < DataEmissao) e.Add("Validade anterior à emissão.");
        if (ex.Tipo == TipoExigencia.ExameAso && ConclusaoAso is null) e.Add("ASO: informe a conclusão (apto/inapto).");
        if (ex.Tipo == TipoExigencia.ExameAso) ArquivoRestrito = true;
        return e;
    }

    public void Decidir(bool valido, string? motivo, string por, DateTimeOffset agora)
    {
        if (!valido && string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo da recusa (vai para a empresa).");
        Validacao = valido ? ValidacaoDoc.Validado : ValidacaoDoc.Recusado;
        MotivoRecusa = valido ? null : motivo; ValidadoPor = por; ValidadoEm = agora;
    }
}

// ═══════════════════════ S05/S06/S07 — Treinamentos ═══════════════════════

public class Treinamento : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public TipoTreinamento Tipo { get; set; } = TipoTreinamento.NR;
    public Guid? DocumentoId { get; set; }
    public string CargaHoraria { get; set; } = string.Empty;
    public string? Conteudo { get; set; }
    public int? ValidadeMeses { get; set; }
    public MetodoEficaciaTreinamento MetodoEficacia { get; set; } = MetodoEficaciaTreinamento.Prova;
    public bool Ativo { get; set; } = true;
}

public class Turma : EntidadeAuditavel, IHistoricoTemporal
{
    public string Codigo { get; set; } = string.Empty;
    public Guid TreinamentoId { get; set; }
    public Guid? ObraId { get; set; }
    public string? Origem { get; set; }
    public DateTimeOffset Data { get; set; }
    public string Instrutor { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public StatusTurma Status { get; set; } = StatusTurma.Programada;
    public ICollection<Participante> Participantes { get; set; } = [];

    /// <summary>Conclui: presença + avaliação viram registro de competência com validade.</summary>
    public void Concluir(Treinamento t)
    {
        if (Status != StatusTurma.Programada) throw new RegraDeNegocioException("Turma já concluída ou cancelada.");
        if (Participantes.Count == 0) throw new RegraDeNegocioException("Inclua os participantes.");
        foreach (var p in Participantes)
        {
            if (!p.Presenca) { p.Resultado = ResultadoParticipante.NaoCompareceu; p.ValidoAte = null; continue; }
            if (t.MetodoEficacia != MetodoEficaciaTreinamento.NaoSeAplica && string.IsNullOrWhiteSpace(p.Avaliacao))
                throw new RegraDeNegocioException($"Informe a avaliação de eficácia de todos os presentes (SiAC 7.2).");
            p.Resultado ??= ResultadoParticipante.Aprovado;
            var data = DateOnly.FromDateTime(Data.Date);
            p.ValidoAte = p.Resultado == ResultadoParticipante.Aprovado ? (t.ValidadeMeses is { } m ? data.AddMonths(m) : DateOnly.MaxValue) : null;
        }
        Status = StatusTurma.Concluida;
    }
}

public class Participante : Entidade
{
    public Guid TurmaId { get; set; }
    public Guid ColaboradorId { get; set; }
    public bool Presenca { get; set; }
    public string? Avaliacao { get; set; }
    public ResultadoParticipante? Resultado { get; set; }
    public DateOnly? ValidoAte { get; set; }
}

// ═══════════════════════ S08/S08A — EPI ═══════════════════════

public class Epi : EntidadeAuditavel
{
    public string Descricao { get; set; } = string.Empty;
    public string Ca { get; set; } = string.Empty;
    public DateOnly ValidadeCa { get; set; }
    public int? VidaUtilDias { get; set; }
    public Guid? ProdutoId { get; set; }
    public bool Ativo { get; set; } = true;
}

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

// ═══════════════════════ S09/S10 — Inspeções de segurança ═══════════════════════

public class InspecaoSeguranca : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public Guid ModeloInspecaoId { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public Guid LocalId { get; set; }
    public string InspetorNome { get; set; } = string.Empty;
    public DateTimeOffset Data { get; set; }
    public decimal? Conformidade { get; set; }
    public string? Paralisacao { get; set; }
    public StatusInspecaoSst Status { get; set; } = StatusInspecaoSst.EmAndamento;
    public ICollection<InspecaoSegurancaItem> Itens { get; set; } = [];

    public IReadOnlyList<string> Concluir()
    {
        var e = new List<string>();
        if (Itens.Any(i => i.Resultado == ResultadoItem.NaoAvaliado)) e.Add("Avalie todos os itens.");
        foreach (var i in Itens.Where(i => i.Resultado == ResultadoItem.NaoConforme))
        {
            if (i.Gravidade is null) e.Add($"Item {i.Ordem}: informe a gravidade.");
            if (string.IsNullOrWhiteSpace(i.Responsavel)) e.Add($"Item {i.Ordem}: informe o responsável / empresa.");
            if (i.Prazo is null) e.Add($"Item {i.Ordem}: informe o prazo.");
        }
        if (Itens.Any(i => i.Gravidade == Gravidade.RiscoGraveIminente) && string.IsNullOrWhiteSpace(Paralisacao))
            e.Add("Risco grave e iminente: registre a paralisação da frente (NR-03).");
        if (e.Count > 0) return e;
        var avaliados = Itens.Count(i => i.Resultado != ResultadoItem.NaoAplica);
        Conformidade = avaliados == 0 ? null : Math.Round(100m * Itens.Count(i => i.Resultado == ResultadoItem.Conforme) / avaliados, 1);
        Status = Itens.Any(i => i.Resultado == ResultadoItem.NaoConforme && !i.Corrigido) ? StatusInspecaoSst.EmTratamento : StatusInspecaoSst.Concluida;
        return e;
    }
}

public class InspecaoSegurancaItem : Entidade
{
    public Guid InspecaoSegurancaId { get; set; }
    public Guid ItemVerificacaoId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public ResultadoItem Resultado { get; set; }
    public Gravidade? Gravidade { get; set; }
    public string? Responsavel { get; set; }
    public DateOnly? Prazo { get; set; }
    public string? Observacao { get; set; }
    public bool Corrigido { get; set; }
    public Guid? RncId { get; set; }
}

// ═══════════════════════ S11/S12 — Permissão de trabalho ═══════════════════════

public class PermissaoTrabalho : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public TipoPermissaoTrabalho Tipo { get; set; }
    public Guid LocalId { get; set; }
    public string Atividade { get; set; } = string.Empty;
    public Guid FornecedorId { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fim { get; set; }
    public string Medidas { get; set; } = string.Empty;          // itens separados por ';'
    public string MedidasMarcadas { get; set; } = string.Empty;  // índices marcados separados por ';'
    public string Emitente { get; set; } = string.Empty;
    public string ResponsavelArea { get; set; } = string.Empty;
    public StatusPermissaoTrabalho Status { get; set; } = StatusPermissaoTrabalho.Rascunho;
    public DateTimeOffset? EncerradaEm { get; set; }
    public string? EncerradaPor { get; set; }
    public ICollection<ExecutantePt> Executantes { get; set; } = [];

    public static AtividadeRisco Risco(TipoPermissaoTrabalho t) => t switch
    {
        TipoPermissaoTrabalho.Altura => AtividadeRisco.Altura,
        TipoPermissaoTrabalho.AQuente => AtividadeRisco.AQuente,
        TipoPermissaoTrabalho.Eletrico => AtividadeRisco.Eletrico,
        TipoPermissaoTrabalho.EspacoConfinado => AtividadeRisco.EspacoConfinado,
        _ => AtividadeRisco.Icamento
    };

    public static string MedidasPadrao(TipoPermissaoTrabalho t) => t switch
    {
        TipoPermissaoTrabalho.Altura => "Linha de vida / ancoragem inspecionada;Cinto paraquedista com talabarte duplo;Área abaixo isolada e sinalizada;Condições de vento e chuva verificadas;Plano de resgate e equipe disponível",
        TipoPermissaoTrabalho.AQuente => "Área livre de inflamáveis;Extintor no local;Vigia de fogo designado;Biombo / anteparo instalado;Inspeção pós-trabalho (30 min)",
        TipoPermissaoTrabalho.Eletrico => "Circuito desenergizado e bloqueado (LOTO);Ausência de tensão testada;Aterramento temporário;Ferramentas isoladas;Área sinalizada",
        TipoPermissaoTrabalho.EspacoConfinado => "Atmosfera medida (O2, inflamáveis, tóxicos);Ventilação instalada;Vigia designado;Equipe de resgate disponível;Comunicação testada",
        _ => "Plano de rigging aprovado;Equipamento e acessórios inspecionados;Área isolada;Sinaleiro designado;Condições de vento verificadas"
    };

    public IReadOnlyList<string> Liberar(DateTimeOffset agora)
    {
        var e = new List<string>();
        if (Status is not (StatusPermissaoTrabalho.Rascunho or StatusPermissaoTrabalho.AguardandoLiberacao or StatusPermissaoTrabalho.Suspensa)) e.Add("PT não pode ser liberada neste status.");
        var total = Medidas.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        var marcadas = MedidasMarcadas.Split(';', StringSplitOptions.RemoveEmptyEntries).Distinct().Count();
        if (total == 0 || marcadas < total) e.Add("Marque todas as medidas de controle para liberar.");
        if (Fim <= Inicio) e.Add("Validade: fim deve ser posterior ao início.");
        if ((Fim - Inicio).TotalHours > 14) e.Add("Validade máxima: um turno do dia.");
        if (Fim < agora) e.Add("Validade já encerrada.");
        if (string.IsNullOrWhiteSpace(Emitente) || string.IsNullOrWhiteSpace(ResponsavelArea)) e.Add("Emitente e responsável da área devem assinar.");
        if (Executantes.Count == 0) e.Add("Inclua os executantes.");
        if (Executantes.Any(x => !x.Liberado)) e.Add("Há executante que não pode entrar: retire-o ou regularize as exigências.");
        if (e.Count == 0) Status = StatusPermissaoTrabalho.Liberada;
        return e;
    }
}

public class ExecutantePt : Entidade
{
    public Guid PermissaoTrabalhoId { get; set; }
    public Guid ColaboradorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public PapelPt Papel { get; set; } = PapelPt.Executante;
    public bool Liberado { get; set; }
    public string? Motivo { get; set; }
}

// ═══════════════════════ S13 — Ocorrências ═══════════════════════

public class Ocorrencia : EntidadeDaObra, IHistoricoTemporal
{
    public string Numero { get; set; } = string.Empty;
    public DateTimeOffset DataHora { get; set; }
    public TipoOcorrencia Tipo { get; set; }
    public Guid LocalId { get; set; }
    public Guid? FornecedorId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int DiasPerdidos { get; set; }
    public string? Cat { get; set; }
    public string? ArquivoRestritoBlob { get; set; }
    public Guid? PermissaoTrabalhoId { get; set; }
    public Guid? InspecaoSegurancaId { get; set; }
    public Guid? AcaoCorretivaId { get; set; }
    public StatusOcorrencia Status { get; set; } = StatusOcorrencia.Registrada;

    public bool Acidente => Tipo is TipoOcorrencia.AcidenteSemAfastamento or TipoOcorrencia.AcidenteComAfastamento;

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(Descricao)) e.Add("Descreva a ocorrência (sem dados médicos).");
        if (Tipo == TipoOcorrencia.AcidenteComAfastamento && DiasPerdidos <= 0) e.Add("Acidente com afastamento: informe os dias perdidos.");
        if (Acidente && string.IsNullOrWhiteSpace(Cat)) e.Add("Acidente: informe a referência da CAT.");
        if (Status == StatusOcorrencia.Encerrada && Acidente && AcaoCorretivaId is null) e.Add("Acidente só encerra com investigação (ação corretiva).");
        return e;
    }
}

// ═══════════════════════ Regras ═══════════════════════

/// <summary>Resultado da análise das exigências de uma pessoa.</summary>
public sealed record SituacaoCalculada(SituacaoColaborador Situacao, AtividadeRisco Restricoes, IReadOnlyList<(string Exigencia, string Situacao, DateOnly? Validade, bool Ok)> Detalhe);

public static class RegrasSst
{
    /// <summary>
    /// Situação da pessoa pelas exigências da função: O vencida/ausente que bloqueia → Bloqueado; C vencida → restringe a atividade;
    /// só alerta → Pendente; tudo em dia → Apto.
    /// </summary>
    public static SituacaoCalculada Calcular(Colaborador c, IEnumerable<FuncaoExigencia> matriz, IReadOnlyDictionary<Guid, Exigencia> exigencias,
        IEnumerable<DocumentoColaborador> docs, IEnumerable<(Guid TreinamentoId, DateOnly? ValidoAte)> treinamentos, bool temEpi, DateOnly hoje)
    {
        if (c.DataSaida is { } s && s <= hoje) return new(SituacaoColaborador.DesligadoDaObra, AtividadeRisco.Nenhuma, []);
        var detalhe = new List<(string, string, DateOnly?, bool)>();
        var bloqueado = false; var pendente = false; var restricoes = AtividadeRisco.Nenhuma;
        foreach (var fe in matriz.Where(m => m.FuncaoObraId == c.FuncaoObraId))
        {
            if (!exigencias.TryGetValue(fe.ExigenciaId, out var ex)) continue;
            DateOnly? validade = null; bool ok; string sit;
            switch (ex.Tipo)
            {
                case TipoExigencia.Treinamento:
                    var t = treinamentos.Where(x => x.TreinamentoId == ex.TreinamentoId).OrderByDescending(x => x.ValidoAte).FirstOrDefault();
                    validade = t.ValidoAte == DateOnly.MaxValue ? null : t.ValidoAte;
                    ok = t.ValidoAte is { } vt && vt >= hoje;
                    sit = t.TreinamentoId == Guid.Empty ? "Não realizado" : ok ? "Válido" : "Vencido";
                    break;
                case TipoExigencia.Epi:
                    ok = temEpi; sit = ok ? "Em dia" : "Sem entrega registrada";
                    break;
                default:
                    var d = docs.Where(x => x.ExigenciaId == ex.Id && x.Validacao == ValidacaoDoc.Validado).OrderByDescending(x => x.DataEmissao).FirstOrDefault();
                    validade = d?.Validade;
                    ok = d is not null && (d.Validade is null || d.Validade >= hoje) && (ex.Tipo != TipoExigencia.ExameAso || d.ConclusaoAso == ConclusaoAso.Apto);
                    sit = d is null ? (docs.Any(x => x.ExigenciaId == ex.Id && x.Validacao == ValidacaoDoc.Aguardando) ? "Em validação" : "Pendente")
                        : ex.Tipo == TipoExigencia.ExameAso && d.ConclusaoAso == ConclusaoAso.Inapto ? "Inapto"
                        : ok ? (ex.Tipo == TipoExigencia.ExameAso ? "Apto · válido" : "Válido") : "Vencido";
                    if (ex.Tipo == TipoExigencia.ExameAso && d is not null && ok)
                        restricoes |= AtividadeRiscoTodas & ~d.AptoAtividades & AtividadesExigidas(matriz, exigencias, c.FuncaoObraId);
                    break;
            }
            detalhe.Add((ex.Descricao, sit, validade, ok));
            if (ok) continue;
            if (fe.Obrigatoriedade == Obrigatoriedade.ConformeAtividade || ex.EfeitoVencimento == EfeitoVencimento.RestringeAtividade)
                restricoes |= ex.Atividades == AtividadeRisco.Nenhuma ? AtividadeRisco.Nenhuma : ex.Atividades;
            else if (ex.EfeitoVencimento == EfeitoVencimento.BloqueiaPessoa) bloqueado = true;
            else pendente = true;
        }
        var situacao = bloqueado ? SituacaoColaborador.Bloqueado
            : restricoes != AtividadeRisco.Nenhuma ? SituacaoColaborador.AptoComRestricao
            : pendente ? SituacaoColaborador.Pendente : SituacaoColaborador.Apto;
        return new(situacao, restricoes, detalhe);
    }

    private const AtividadeRisco AtividadeRiscoTodas = AtividadeRisco.Altura | AtividadeRisco.EspacoConfinado | AtividadeRisco.Eletrico | AtividadeRisco.OperacaoEquipamento;

    private static AtividadeRisco AtividadesExigidas(IEnumerable<FuncaoExigencia> matriz, IReadOnlyDictionary<Guid, Exigencia> ex, Guid funcao) =>
        matriz.Where(m => m.FuncaoObraId == funcao && ex.ContainsKey(m.ExigenciaId))
              .Aggregate(AtividadeRisco.Nenhuma, (a, m) => a | ex[m.ExigenciaId].Atividades);

    /// <summary>Executante só entra na PT se apto e sem restrição na atividade do tipo de PT.</summary>
    public static (bool Liberado, string? Motivo) PodeExecutar(Colaborador c, TipoPermissaoTrabalho tipo)
    {
        if (c.Situacao is SituacaoColaborador.Bloqueado or SituacaoColaborador.DesligadoDaObra) return (false, $"Colaborador {De(c.Situacao)}.");
        var risco = PermissaoTrabalho.Risco(tipo);
        if ((c.Restricoes & risco) != 0) return (false, "Exigência da atividade vencida ou ausente.");
        return (true, null);
    }

    private static string De(SituacaoColaborador s) => s switch { SituacaoColaborador.Bloqueado => "bloqueado", SituacaoColaborador.DesligadoDaObra => "desligado da obra", _ => s.ToString() };

    /// <summary>Taxa de frequência (NBR 14280): acidentes com afastamento × 1.000.000 / horas-homem.</summary>
    public static decimal? TaxaFrequencia(int acidentesComAfastamento, decimal horasHomem) =>
        horasHomem <= 0 ? null : Math.Round(acidentesComAfastamento * 1_000_000m / horasHomem, 2);
}
