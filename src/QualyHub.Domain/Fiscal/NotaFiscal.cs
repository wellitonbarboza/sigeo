using System.Globalization;
using System.Text;
using QualyHub.Domain.Common;

namespace QualyHub.Domain.Fiscal;

public enum SituacaoNota { Recebida = 1, Vinculada = 2, Recebimento = 3, Cancelada = 4, Ignorada = 5 }
public enum OrigemNota { Manual = 1, Sieg = 2 }
/// <summary>Quem ligou a nota/item ao pedido: regra automática (CNPJ, nº do pedido, descrição), IA (sugestão aceita) ou pessoa.</summary>
public enum OrigemVinculo { Regra = 1, Ia = 2, Manual = 3 }

/// <summary>
/// NF-e de entrada (fornecedor → CNPJ da construtora), importada do XML (SIEG ou arquivo).
/// Fluxo: recebida → vinculada ao pedido (regra/IA/pessoa) → recebimento registrado (FVM). Cancelada pela SEFAZ não segue.
/// Não pertence a uma obra na chegada: a obra vem do pedido vinculado.
/// </summary>
public class NotaFiscalEntrada : EntidadeAuditavel
{
    public string Chave { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Modelo { get; set; } = "55";
    public DateTimeOffset Emissao { get; set; }
    public string EmitenteCnpj { get; set; } = string.Empty;
    public string EmitenteNome { get; set; } = string.Empty;
    public string DestinatarioCnpj { get; set; } = string.Empty;
    public string? DestinatarioNome { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal? ValorFrete { get; set; }
    /// <summary>Informações complementares (infCpl): costuma trazer o nº do pedido do cliente.</summary>
    public string? InformacoesComplementares { get; set; }
    /// <summary>Números de pedido citados na nota (xPed dos itens + texto), separados por vírgula.</summary>
    public string? PedidosCitados { get; set; }
    public SituacaoNota Situacao { get; set; } = SituacaoNota.Recebida;
    public OrigemNota Origem { get; set; } = OrigemNota.Manual;
    /// <summary>Caminho do XML no armazenamento (Blob/pasta local).</summary>
    public string? ArquivoXml { get; set; }
    public Guid? FornecedorId { get; set; }
    public Guid? PedidoId { get; set; }
    public Guid? ObraId { get; set; }
    public Guid? RecebimentoId { get; set; }
    public OrigemVinculo? VinculoOrigem { get; set; }
    /// <summary>Por que foi (ou não) vinculada — mostrado na tela para conferência.</summary>
    public string? VinculoDetalhe { get; set; }
    /// <summary>Última análise da IA (texto para o comprador conferir; não altera nada sozinha).</summary>
    public string? AnaliseIa { get; set; }
    public ICollection<NotaFiscalItem> Itens { get; set; } = [];

    public bool PodeVincular => Situacao is SituacaoNota.Recebida or SituacaoNota.Vinculada;

    /// <summary>Liga ao pedido (a obra vem dele). Só nota ainda não recebida nem cancelada/ignorada.</summary>
    public void VincularPedido(Guid pedidoId, Guid obraId, OrigemVinculo origem, string detalhe)
    {
        if (!PodeVincular) throw new InvalidOperationException("Nota já recebida, cancelada ou ignorada.");
        PedidoId = pedidoId; ObraId = obraId; VinculoOrigem = origem; VinculoDetalhe = detalhe;
        Situacao = SituacaoNota.Vinculada;
    }

    public void Desvincular(string motivo)
    {
        if (!PodeVincular) throw new InvalidOperationException("Nota já recebida, cancelada ou ignorada.");
        PedidoId = null; ObraId = null; VinculoOrigem = null; VinculoDetalhe = motivo; Situacao = SituacaoNota.Recebida;
        foreach (var i in Itens) { i.PedidoItemId = null; i.VinculoOrigem = null; i.Confianca = null; }
    }
}

public class NotaFiscalItem : Entidade
{
    public Guid NotaFiscalId { get; set; }
    public int Item { get; set; }
    public string? Codigo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Valor { get; set; }
    /// <summary>xPed do item (nº do pedido do cliente) e nItemPed, quando o fornecedor informa.</summary>
    public string? PedidoCitado { get; set; }
    public string? ItemPedidoCitado { get; set; }
    public Guid? PedidoItemId { get; set; }
    public OrigemVinculo? VinculoOrigem { get; set; }
    /// <summary>0–1: semelhança da descrição com o item do pedido (regra) ou confiança informada pela IA.</summary>
    public decimal? Confianca { get; set; }
}

/// <summary>Registro de cada busca de notas (SIEG ou arquivo): o que veio, o que entrou e o erro, se houve.</summary>
public class ImportacaoNota : EntidadeAuditavel
{
    public OrigemNota Origem { get; set; }
    public string? Cnpj { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public int Lidas { get; set; }
    public int Novas { get; set; }
    public int Canceladas { get; set; }
    public string? Detalhe { get; set; }
    public bool Falhou { get; set; }
}

/// <summary>Item do pedido candidato ao vínculo com o item da nota.</summary>
public sealed record ItemPedidoParaConferencia(Guid Id, string Descricao, string? Unidade, decimal Saldo, string? Codigo);

/// <summary>
/// Regra de conferência nota × pedido sem IA: semelhança de palavras da descrição (sem acento, sem palavras curtas),
/// código do insumo igual, unidade igual e quantidade dentro do saldo. Cada item do pedido é usado uma vez.
/// </summary>
public static class ConferenciaNota
{
    public const decimal ConfiancaMinima = 0.35m;

    public static Dictionary<int, (Guid PedidoItemId, decimal Confianca)> Parear(IReadOnlyList<NotaFiscalItem> nota, IReadOnlyList<ItemPedidoParaConferencia> pedido)
    {
        var pares = new List<(int Item, Guid Ped, decimal Nota)>();
        foreach (var n in nota)
            foreach (var p in pedido)
            {
                var s = Semelhanca(n.Descricao, p.Descricao);
                if (!string.IsNullOrWhiteSpace(n.Codigo) && string.Equals(n.Codigo?.Trim(), p.Codigo?.Trim(), StringComparison.OrdinalIgnoreCase)) s = Math.Max(s, 0.9m);
                if (MesmaUnidade(n.Unidade, p.Unidade)) s += 0.1m;
                if (p.Saldo > 0 && n.Quantidade > p.Saldo * 1.05m) s -= 0.1m;
                if (s >= ConfiancaMinima) pares.Add((n.Item, p.Id, Math.Min(1, Math.Round(s, 2))));
            }
        var r = new Dictionary<int, (Guid, decimal)>();
        var usados = new HashSet<Guid>();
        foreach (var (item, ped, nota_) in pares.OrderByDescending(x => x.Nota))
            if (!r.ContainsKey(item) && usados.Add(ped)) r[item] = (ped, nota_);
        return r;
    }

    /// <summary>Coeficiente de Jaccard das palavras significativas (0–1).</summary>
    public static decimal Semelhanca(string? a, string? b)
    {
        var x = Palavras(a); var y = Palavras(b);
        if (x.Count == 0 || y.Count == 0) return 0;
        var inter = x.Count(y.Contains);
        return Math.Round((decimal)inter / (x.Count + y.Count - inter), 4);
    }

    public static bool MesmaUnidade(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && Normalizar(a) == Normalizar(b);

    private static HashSet<string> Palavras(string? s) =>
        Normalizar(s).Split([' ', '-', '/', ',', '.', '(', ')', ';', ':', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2 || p.Any(char.IsDigit)).ToHashSet();

    private static string Normalizar(string? s)
    {
        var d = (s ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString().Trim();
    }
}
