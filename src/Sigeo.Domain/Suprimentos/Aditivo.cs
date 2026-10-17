using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Suprimentos;

/// <summary>T28 · Aditivo de contrato (valor, prazo, escopo). Aprovado e assinado, atualiza itens, vigência e saldo da medição.</summary>
public class Aditivo : EntidadeDaObra, IHistoricoTemporal
{
    public Guid ContratoId { get; set; }
    public Contrato? Contrato { get; set; }
    public string Numero { get; set; } = string.Empty;
    public TipoAditivo Tipo { get; set; } = TipoAditivo.Valor;
    public string? Origem { get; set; }
    public string Justificativa { get; set; } = string.Empty;
    public DateOnly? NovaVigencia { get; set; }
    public decimal Valor { get; set; }
    public decimal PercAcumulado { get; set; }
    public Guid? SolicitacaoAprovacaoId { get; set; }
    public string? AprovadoPor { get; set; }
    public DateTimeOffset? AprovadoEm { get; set; }
    public DateOnly? AssinaturaEmpresa { get; set; }
    public DateOnly? AssinaturaFornecedor { get; set; }
    public StatusAditivo Status { get; set; } = StatusAditivo.EmElaboracao;
    public ICollection<AditivoItem> Itens { get; set; } = [];

    public bool AlteraValor => Tipo is TipoAditivo.Valor or TipoAditivo.Escopo or TipoAditivo.Combinado;
    public bool AlteraPrazo => Tipo is TipoAditivo.Prazo or TipoAditivo.Combinado;

    /// <summary>Calcula valor do aditivo e % acumulado sobre o valor original do contrato.</summary>
    public void Calcular(decimal valorOriginal, decimal aditivosAnteriores)
    {
        Valor = Itens.Sum(i => i.ValorDelta);
        PercAcumulado = valorOriginal == 0 ? 0 : Math.Round(100m * (aditivosAnteriores + Valor) / valorOriginal, 2);
    }

    public IReadOnlyList<string> Validar(Contrato k)
    {
        var e = new List<string>();
        if (k.Status != StatusContrato.Ativo) e.Add("Aditivo só em contrato ativo.");
        if (string.IsNullOrWhiteSpace(Justificativa)) e.Add("Informe a justificativa.");
        if (AlteraValor && Itens.Count == 0) e.Add("Aditivo de valor/escopo: informe os itens alterados.");
        if (AlteraPrazo && (NovaVigencia is null || NovaVigencia <= k.VigenciaFim)) e.Add("Aditivo de prazo: nova vigência deve ser posterior à atual.");
        foreach (var i in Itens)
        {
            if (i.ContratoItemId is null && (string.IsNullOrWhiteSpace(i.Descricao) || i.QtdDelta <= 0)) e.Add("Item novo: informe descrição e quantidade.");
            var ci = k.Itens.FirstOrDefault(x => x.Id == i.ContratoItemId);
            if (ci is not null && ci.Quantidade + i.QtdDelta < 0) e.Add($"Item {ci.Item}: supressão maior que a quantidade contratada.");
        }
        return e;
    }

    /// <summary>Vigora após aprovação e assinaturas: aplica quantidades/preços, itens novos e nova vigência.</summary>
    public void Aplicar(Contrato k)
    {
        if (AprovadoEm is null) throw new Aprovacoes.RegraDeNegocioException("Aditivo sem aprovação.");
        if (AssinaturaEmpresa is null || AssinaturaFornecedor is null) throw new Aprovacoes.RegraDeNegocioException("Informe as assinaturas da empresa e do fornecedor.");
        foreach (var i in Itens)
        {
            var ci = k.Itens.FirstOrDefault(x => x.Id == i.ContratoItemId);
            if (ci is null)
                k.Itens.Add(new ContratoItem { ContratoId = k.Id, Item = i.Item ?? $"A{k.Itens.Count + 1}", Descricao = i.Descricao ?? "", Unidade = i.Unidade ?? "un", Quantidade = i.QtdDelta, PrecoUnitario = i.PrecoUnitario, ServicoControladoId = i.ServicoControladoId });
            else { ci.Quantidade += i.QtdDelta; ci.PrecoUnitario = i.PrecoUnitario; }
        }
        if (AlteraPrazo && NovaVigencia is { } v) k.VigenciaFim = v;
        Status = StatusAditivo.Vigente;
    }
}
