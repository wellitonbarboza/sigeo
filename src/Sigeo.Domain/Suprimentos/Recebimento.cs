using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

/// <summary>
/// T13 · Entrada de material + FVM (almoxarife). Avalia o fornecedor na entrega (prazo, qualidade, atendimento).
/// FVM reprovada → ocorrência obrigatória, comunicado e plano de ação pontual do fornecedor.
/// </summary>
public class Recebimento : EntidadeDaObra, IHistoricoTemporal
{
    public int Numero { get; set; }
    public Guid? PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public Guid? ContratoId { get; set; }
    public Guid FornecedorId { get; set; }
    /// <summary>Recebido no almoxarifado central (depois transferido para a obra/SPE).</summary>
    public bool AlmoxarifadoCentral { get; set; }
    /// <summary>Onde está sendo recebido (almoxarifado central ou da obra): o material entra no estoque dele.</summary>
    public Guid? AlmoxarifadoId { get; set; }
    public Guid? EstabelecimentoId { get; set; }
    public string? ChaveNFe { get; set; }
    public string? NumeroNotaFiscal { get; set; }
    public DateTimeOffset DataRecebimento { get; set; }
    public Guid? RecebidoPorUsuarioId { get; set; }
    public string RecebidoPorNome { get; set; } = string.Empty;
    public ResultadoCriterio FvmPrazo { get; set; }
    public ResultadoCriterio FvmQualidade { get; set; }
    public ResultadoCriterio FvmAtendimento { get; set; }
    /// <summary>Notas de 0 a 10 da entrega (avaliação do fornecedor). Quando informadas, definem o resultado de cada critério e o IDF.</summary>
    public decimal? NotaPrazo { get; set; }
    public decimal? NotaQualidade { get; set; }
    /// <summary>Nota do 3º critério (documentação / atendimento).</summary>
    public decimal? NotaDocumentacao { get; set; }
    /// <summary>Índice de desempenho do fornecedor na entrega (média ponderada das 3 notas).</summary>
    public decimal? Idf { get; set; }
    public string? Ocorrencia { get; set; }
    public DestinoRecebimento Destino { get; set; } = DestinoRecebimento.Aceito;
    public string? ComunicadoA { get; set; }
    public DateTimeOffset? ComunicadoEm { get; set; }
    public Guid? ModeloFvmId { get; set; }
    public StatusLote StatusLote { get; set; } = StatusLote.EmVerificacao;
    public bool QuarentenaAteEnsaio { get; set; }
    public bool RastrearAplicacao { get; set; }
    public Guid? RncId { get; set; }
    public ICollection<Qualidade.FvmItem> FvmItens { get; set; } = [];
    public ICollection<Qualidade.EnsaioRecebimento> Ensaios { get; set; } = [];
    public ICollection<Qualidade.AplicacaoLote> Aplicacoes { get; set; } = [];

    /// <summary>
    /// Avaliação por nota (0–10): nota ≥ mínimo aprova o critério, abaixo reprova. IDF = média ponderada pelos pesos
    /// (só com as 3 notas). Rodar antes do checklist: item bloqueante não conforme continua reprovando a qualidade.
    /// </summary>
    public void AplicarNotas(decimal pesoPrazo, decimal pesoQualidade, decimal pesoDocumentacao, decimal minimo)
    {
        foreach (var n in new[] { NotaPrazo, NotaQualidade, NotaDocumentacao })
            if (n is < 0 or > 10) throw new Aprovacoes.RegraDeNegocioException("As notas da avaliação vão de 0 a 10.");
        static ResultadoCriterio R(decimal? n, decimal min, ResultadoCriterio atual) => n is null ? atual : n >= min ? ResultadoCriterio.Aprovado : ResultadoCriterio.Reprovado;
        FvmPrazo = R(NotaPrazo, minimo, FvmPrazo);
        FvmQualidade = R(NotaQualidade, minimo, FvmQualidade);
        FvmAtendimento = R(NotaDocumentacao, minimo, FvmAtendimento);
        var soma = pesoPrazo + pesoQualidade + pesoDocumentacao;
        Idf = NotaPrazo is { } p && NotaQualidade is { } q && NotaDocumentacao is { } d && soma > 0
            ? Math.Round((pesoPrazo * p + pesoQualidade * q + pesoDocumentacao * d) / soma, 2)
            : null;
    }

    /// <summary>Checklist do modelo com item bloqueante não conforme reprova a FVM (critério "qualidade").</summary>
    public void AplicarChecklist()
    {
        if (FvmItens.Count == 0) return;
        foreach (var i in FvmItens.Where(i => i.ValorMin is not null || i.ValorMax is not null))
            if (decimal.TryParse(i.ValorMedido?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                i.Resultado = (i.ValorMin is null || v >= i.ValorMin) && (i.ValorMax is null || v <= i.ValorMax) ? ResultadoItem.Conforme : ResultadoItem.NaoConforme;
        if (FvmItens.Any(i => i.Bloqueia && i.Resultado == ResultadoItem.NaoConforme)) FvmQualidade = ResultadoCriterio.Reprovado;
        else if (FvmItens.All(i => i.Resultado != ResultadoItem.NaoAvaliado) && FvmQualidade == ResultadoCriterio.NaoAvaliado) FvmQualidade = ResultadoCriterio.Aprovado;
    }

    public decimal QuantidadeRecebida => Itens.Sum(i => i.QtdRecebida);

    public ICollection<RecebimentoItem> Itens { get; set; } = [];

    /// <summary>Aprovado se os 3 critérios forem aprovados; nulo enquanto algum não foi avaliado.</summary>
    public bool? FvmAprovada =>
        FvmPrazo == ResultadoCriterio.NaoAvaliado || FvmQualidade == ResultadoCriterio.NaoAvaliado || FvmAtendimento == ResultadoCriterio.NaoAvaliado
            ? null
            : FvmPrazo == ResultadoCriterio.Aprovado && FvmQualidade == ResultadoCriterio.Aprovado && FvmAtendimento == ResultadoCriterio.Aprovado;

    /// <summary>Nota da entrega (0–10) usada na avaliação consolidada: % de critérios aprovados.</summary>
    public decimal? NotaEntrega => Idf ?? (FvmAprovada is null ? null
        : Math.Round(10m * new[] { FvmPrazo, FvmQualidade, FvmAtendimento }.Count(c => c == ResultadoCriterio.Aprovado) / 3m, 2));

    public bool TemDivergencia => Itens.Any(i => i.QtdRecebida != i.QtdPedida);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (PedidoId is null && ContratoId is null) e.Add("Informe o pedido ou contrato.");
        if (string.IsNullOrWhiteSpace(ChaveNFe) && string.IsNullOrWhiteSpace(NumeroNotaFiscal)) e.Add("Informe a nota fiscal (chave ou número).");
        if (ChaveNFe is { Length: > 0 } c && (c.Length != 44 || !c.All(char.IsDigit))) e.Add("Chave da NF-e deve ter 44 dígitos.");
        if (Itens.Count == 0) e.Add("Informe os itens recebidos.");
        if (FvmAprovada is null) e.Add("Avalie os 3 critérios da FVM (prazo, qualidade, atendimento).");
        if ((FvmAprovada == false || TemDivergencia) && string.IsNullOrWhiteSpace(Ocorrencia))
            e.Add("FVM reprovada ou quantidade divergente: descreva a ocorrência / não conformidade.");
        foreach (var i in Itens.Where(i => i.MaterialControlado))
        {
            if (string.IsNullOrWhiteSpace(i.Lote)) e.Add($"{i.Descricao}: material controlado exige lote/corrida.");
            if (!i.DocumentosRecebidos) e.Add($"{i.Descricao}: material controlado exige os documentos (certificado, laudo, FISPQ…).");
        }
        foreach (var i in FvmItens.Where(i => i.Resultado == ResultadoItem.NaoAvaliado))
            e.Add($"FVM: avalie o item {i.Ordem} ({i.Descricao}).");
        foreach (var i in FvmItens.Where(i => i.Resultado is ResultadoItem.NaoConforme or ResultadoItem.NaoAplica && string.IsNullOrWhiteSpace(i.Observacao)))
            e.Add($"FVM item {i.Ordem}: observação obrigatória para Não conforme / N/A.");
        if (Destino == DestinoRecebimento.Aceito && FvmAprovada == false)
            e.Add("FVM reprovada: destino deve ser 'Aceito com ressalva' ou 'Devolvido'.");
        return e;
    }
}
