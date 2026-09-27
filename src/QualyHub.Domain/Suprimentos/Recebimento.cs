using QualyHub.Domain.Common;

namespace QualyHub.Domain.Suprimentos;

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
    public string? ChaveNFe { get; set; }
    public string? NumeroNotaFiscal { get; set; }
    public DateTimeOffset DataRecebimento { get; set; }
    public Guid? RecebidoPorUsuarioId { get; set; }
    public string RecebidoPorNome { get; set; } = string.Empty;
    public ResultadoCriterio FvmPrazo { get; set; }
    public ResultadoCriterio FvmQualidade { get; set; }
    public ResultadoCriterio FvmAtendimento { get; set; }
    public string? Ocorrencia { get; set; }
    public DestinoRecebimento Destino { get; set; } = DestinoRecebimento.Aceito;
    public string? ComunicadoA { get; set; }
    public DateTimeOffset? ComunicadoEm { get; set; }
    /// <summary>Ficha FVM detalhada (checklist do modelo), quando o material controlado tiver modelo próprio.</summary>
    public Guid? FichaFvmId { get; set; }

    public ICollection<RecebimentoItem> Itens { get; set; } = [];

    /// <summary>Aprovado se os 3 critérios forem aprovados; nulo enquanto algum não foi avaliado.</summary>
    public bool? FvmAprovada =>
        FvmPrazo == ResultadoCriterio.NaoAvaliado || FvmQualidade == ResultadoCriterio.NaoAvaliado || FvmAtendimento == ResultadoCriterio.NaoAvaliado
            ? null
            : FvmPrazo == ResultadoCriterio.Aprovado && FvmQualidade == ResultadoCriterio.Aprovado && FvmAtendimento == ResultadoCriterio.Aprovado;

    /// <summary>Nota da entrega (0–10) usada na avaliação consolidada: % de critérios aprovados.</summary>
    public decimal? NotaEntrega => FvmAprovada is null ? null
        : Math.Round(10m * new[] { FvmPrazo, FvmQualidade, FvmAtendimento }.Count(c => c == ResultadoCriterio.Aprovado) / 3m, 2);

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
        if (Destino == DestinoRecebimento.Aceito && FvmAprovada == false)
            e.Add("FVM reprovada: destino deve ser 'Aceito com ressalva' ou 'Devolvido'.");
        return e;
    }
}

public class RecebimentoItem : Entidade
{
    public Guid RecebimentoId { get; set; }
    public Recebimento Recebimento { get; set; } = null!;
    public Guid? PedidoItemId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Unidade { get; set; }
    public decimal QtdPedida { get; set; }
    public decimal QtdRecebida { get; set; }
    public bool MaterialControlado { get; set; }
    public Guid? MaterialControladoId { get; set; }
    public string? Lote { get; set; }
    public bool DocumentosRecebidos { get; set; }
}
