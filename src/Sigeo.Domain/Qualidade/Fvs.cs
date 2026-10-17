using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Qualidade;

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
