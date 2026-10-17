using Sigeo.Domain.Common;

namespace Sigeo.Domain.Suprimentos;

public enum SituacaoLote { EmCompra = 1, EmRecebimento = 2, Atendido = 3, Encerrado = 4 }

/// <summary>
/// Parte da requisição que cabe a um comprador (itens da mesma carteira). Nasce na autorização da requisição pelo
/// encaminhamento automático; cada lote segue sozinho (cotação, pedidos, recebimentos).
/// </summary>
public class LoteCompra : EntidadeDaObra
{
    public Guid RequisicaoId { get; set; }
    /// <summary>Letra do lote dentro da requisição (A, B, C…).</summary>
    public string Letra { get; set; } = "A";
    /// <summary>Categoria que gerou o lote (grupo, grupo › subgrupo ou categoria de serviço).</summary>
    public string Categoria { get; set; } = string.Empty;
    /// <summary>Comprador atual; nulo = fila do gestor de suprimentos.</summary>
    public Guid? CompradorUsuarioId { get; set; }
    public string? CompradorNome { get; set; }
    /// <summary>Titular da carteira quando o lote está com o substituto (ausência).</summary>
    public Guid? TitularUsuarioId { get; set; }
    public string? TitularNome { get; set; }
    public SituacaoLote Situacao { get; set; } = SituacaoLote.EmCompra;
    /// <summary>Data limite para comprar: necessidade − lead time do item mais demorado.</summary>
    public DateOnly? DataLimiteCompra { get; set; }
    public DateTimeOffset AtribuidoEm { get; set; }
    public string? MotivoReatribuicao { get; set; }

    public bool ComSubstituto => TitularUsuarioId is not null && TitularUsuarioId != CompradorUsuarioId;
    public bool NaFilaDoGestor => CompradorUsuarioId is null;
}

/// <summary>Carteira de compras de um comprador: categorias que compra, obras, substituto e ausências.</summary>
public class CarteiraComprador : EntidadeAuditavel
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? SubstitutoUsuarioId { get; set; }
    public string? SubstitutoNome { get; set; }
    /// <summary>Opcional: acima deste número de lotes abertos o gestor é avisado.</summary>
    public int? LimiteCarga { get; set; }
    public bool Ativa { get; set; } = true;
    public ICollection<CarteiraCategoria> Categorias { get; set; } = [];
    public ICollection<AusenciaComprador> Ausencias { get; set; } = [];

    public bool AusenteEm(DateOnly dia) => Ausencias.Any(a => a.Inicio <= dia && a.Fim >= dia);

    public IReadOnlyList<string> Validar()
    {
        var e = new List<string>();
        if (UsuarioId == Guid.Empty) e.Add("Escolha o usuário comprador.");
        if (SubstitutoUsuarioId == UsuarioId && UsuarioId != Guid.Empty) e.Add("O substituto precisa ser outra pessoa.");
        if (LimiteCarga is < 1) e.Add("Limite de carga precisa ser maior que zero.");
        foreach (var c in Categorias)
            if (string.IsNullOrWhiteSpace(c.Grupo) && string.IsNullOrWhiteSpace(c.CategoriaServico)) e.Add("Categoria sem grupo de produto nem categoria de serviço.");
        foreach (var a in Ausencias)
            if (a.Fim < a.Inicio) e.Add($"Ausência de {a.Inicio:dd/MM/yyyy}: fim antes do início.");
        return e;
    }
}

/// <summary>Categoria da carteira: grupo (e subgrupo opcional) de produto, ou categoria de serviço; obra opcional (vazio = todas).</summary>
public class CarteiraCategoria : Entidade
{
    public Guid CarteiraId { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? CategoriaServico { get; set; }
    public Guid? ObraId { get; set; }

    public string Rotulo => !string.IsNullOrWhiteSpace(CategoriaServico) ? $"Serviço › {CategoriaServico}"
        : string.IsNullOrWhiteSpace(SubGrupo) ? $"{Grupo} · grupo inteiro" : $"{Grupo} › {SubGrupo}";
}

public class AusenciaComprador : Entidade
{
    public Guid CarteiraId { get; set; }
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Na ausência, os lotes ainda não iniciados também vão para o substituto (senão só os novos).</summary>
    public bool TransferirAbertos { get; set; } = true;
}

/// <summary>Quantidades de um item da requisição ao longo do atendimento (calculadas, nunca gravadas).</summary>
public sealed record SaldoItem(Guid ItemId, decimal Requisitado, decimal EmCotacao, decimal Pedido, decimal Recebido, decimal Cancelado)
{
    public decimal APedir => Math.Max(0, Requisitado - Pedido - Cancelado);
    public decimal AReceber => Math.Max(0, Pedido - Recebido);
    /// <summary>O que ainda pode entrar numa nova cotação (desconta o que já está em cotação aberta).</summary>
    public decimal DisponivelCotacao => Math.Max(0, APedir - EmCotacao);
    public bool Atendido => Requisitado > 0 && Recebido + Cancelado >= Requisitado;
    public string Situacao => Cancelado >= Requisitado && Requisitado > 0 ? "Cancelado"
        : Atendido ? (Cancelado > 0 ? "Encerrado com saldo" : "Atendido")
        : Pedido > 0 && APedir == 0 ? "A receber"
        : Pedido > 0 || Recebido > 0 ? "Parcial"
        : EmCotacao > 0 ? "Em cotação" : "A pedir";
}

/// <summary>Regras do atendimento parcial: saldo por item e situação da requisição.</summary>
public static class AtendimentoRequisicao
{
    public sealed record LinhaCotacao(Guid CotacaoId, Guid RequisicaoItemId, decimal Quantidade);
    public sealed record LinhaPedido(Guid RequisicaoItemId, decimal Quantidade, decimal Recebida);

    /// <summary>
    /// Saldo de cada item. Em cotação = quantidade nas cotações ainda abertas (uma vez por cotação, não por fornecedor);
    /// pedido e recebido = soma dos itens de pedido não cancelados ligados ao item.
    /// </summary>
    public static List<SaldoItem> Calcular(IEnumerable<RequisicaoItem> itens, IEnumerable<LinhaCotacao> cotacoesAbertas, IEnumerable<LinhaPedido> pedidos)
    {
        var cot = cotacoesAbertas.GroupBy(c => (c.CotacaoId, c.RequisicaoItemId)).Select(g => (g.Key.RequisicaoItemId, Qtd: g.Max(x => x.Quantidade)))
            .GroupBy(x => x.RequisicaoItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Qtd));
        var ped = pedidos.GroupBy(p => p.RequisicaoItemId).ToDictionary(g => g.Key, g => (Q: g.Sum(x => x.Quantidade), R: g.Sum(x => x.Recebida)));
        return itens.Select(i =>
        {
            var requisitado = i.Status == StatusItemRequisicao.Cancelado && i.QtdCancelada == 0 ? 0 : i.Quantidade;
            var p = ped.GetValueOrDefault(i.Id);
            return new SaldoItem(i.Id, requisitado, cot.GetValueOrDefault(i.Id), p.Q, p.R, Math.Min(i.QtdCancelada, requisitado));
        }).ToList();
    }

    /// <summary>
    /// Situação da requisição a partir dos saldos: sem nada comprado fica como está (autorizada); comprando = em compra;
    /// algo recebido = parcialmente atendida; tudo recebido = atendida; recebido + cancelado = encerrada com saldo.
    /// </summary>
    public static StatusRequisicao Situacao(StatusRequisicao atual, IReadOnlyCollection<SaldoItem> saldos)
    {
        if (atual is StatusRequisicao.Rascunho or StatusRequisicao.AguardandoEngenheiro or StatusRequisicao.AguardandoDiretorTecnico
            or StatusRequisicao.NaoAutorizada or StatusRequisicao.DevolvidaAjuste or StatusRequisicao.Cancelada) return atual;
        var ativos = saldos.Where(s => s.Requisitado > 0).ToList();
        if (ativos.Count == 0) return atual;
        if (ativos.All(s => s.Atendido)) return ativos.Any(s => s.Cancelado > 0) ? StatusRequisicao.EncerradaComSaldo : StatusRequisicao.Atendida;
        if (ativos.Any(s => s.Recebido > 0)) return StatusRequisicao.ParcialmenteAtendida;
        if (ativos.Any(s => s.Pedido > 0 || s.EmCotacao > 0)) return StatusRequisicao.EmCotacao;
        return ativos.Any(s => s.Cancelado > 0) ? StatusRequisicao.ParcialmenteAtendida : StatusRequisicao.Autorizada;
    }

    public static SituacaoLote SituacaoLote(IReadOnlyCollection<SaldoItem> saldosDoLote)
    {
        var ativos = saldosDoLote.Where(s => s.Requisitado > 0).ToList();
        if (ativos.Count == 0 || ativos.All(s => s.Cancelado >= s.Requisitado)) return Suprimentos.SituacaoLote.Encerrado;
        if (ativos.All(s => s.Atendido)) return ativos.Any(s => s.Cancelado > 0) ? Suprimentos.SituacaoLote.Encerrado : Suprimentos.SituacaoLote.Atendido;
        if (ativos.All(s => s.APedir == 0)) return Suprimentos.SituacaoLote.EmRecebimento;
        return Suprimentos.SituacaoLote.EmCompra;
    }

    /// <summary>Encerrar saldo: cancela o que falta pedir do item, com justificativa (fica no histórico).</summary>
    public static decimal EncerrarSaldo(RequisicaoItem item, SaldoItem saldo, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new Aprovacoes.RegraDeNegocioException("Encerrar saldo exige justificativa.");
        if (saldo.EmCotacao > 0) throw new Aprovacoes.RegraDeNegocioException($"{item.Descricao}: há quantidade em cotação aberta. Conclua ou cancele a cotação antes.");
        var falta = saldo.APedir;
        if (falta <= 0) throw new Aprovacoes.RegraDeNegocioException($"{item.Descricao}: não há saldo a pedir.");
        item.QtdCancelada += falta;
        item.MotivoCancelamento = string.IsNullOrWhiteSpace(item.MotivoCancelamento) ? motivo.Trim() : $"{item.MotivoCancelamento} | {motivo.Trim()}";
        return falta;
    }
}

/// <summary>
/// Encaminhamento automático: cada item da requisição autorizada vai para o comprador da sua categoria.
/// Ordem: subgrupo + obra → grupo + obra → subgrupo → grupo (sem obra) → fila do gestor. Ausente: vai ao substituto;
/// substituto também ausente: fila do gestor.
/// </summary>
public static class EncaminhamentoCompras
{
    public sealed record Destino(Guid? CompradorId, string? CompradorNome, Guid? TitularId, string? TitularNome, string Categoria);

    private static string K(string? s) => (s ?? "").Trim().ToUpperInvariant();

    public static Destino Encaminhar(string? grupo, string? subGrupo, string? categoriaServico, Guid obraId, IReadOnlyCollection<CarteiraComprador> carteiras, DateOnly hoje)
    {
        var ativas = carteiras.Where(c => c.Ativa && !c.Excluido).ToList();
        IEnumerable<(CarteiraComprador C, CarteiraCategoria Cat)> Todas() => ativas.SelectMany(c => c.Categorias.Select(cat => (c, cat)));
        (CarteiraComprador C, CarteiraCategoria Cat)? achado = null;
        string categoria;
        if (!string.IsNullOrWhiteSpace(categoriaServico))
        {
            categoria = $"Serviço › {categoriaServico}";
            achado = Todas().Where(x => K(x.Cat.CategoriaServico) == K(categoriaServico)).OrderBy(x => x.Cat.ObraId == obraId ? 0 : x.Cat.ObraId is null ? 1 : 2)
                .Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault(x => x!.Value.Item2.ObraId is null || x.Value.Item2.ObraId == obraId);
        }
        else
        {
            categoria = string.IsNullOrWhiteSpace(subGrupo) ? grupo ?? "Sem grupo" : $"{grupo} › {subGrupo}";
            bool Sub((CarteiraComprador C, CarteiraCategoria Cat) x) => !string.IsNullOrWhiteSpace(x.Cat.SubGrupo) && K(x.Cat.Grupo) == K(grupo) && K(x.Cat.SubGrupo) == K(subGrupo);
            bool Grp((CarteiraComprador C, CarteiraCategoria Cat) x) => string.IsNullOrWhiteSpace(x.Cat.SubGrupo) && string.IsNullOrWhiteSpace(x.Cat.CategoriaServico) && K(x.Cat.Grupo) == K(grupo);
            var l = Todas().ToList();
            achado = l.Where(x => Sub(x) && x.Cat.ObraId == obraId).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Grp(x) && x.Cat.ObraId == obraId).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Sub(x) && x.Cat.ObraId is null).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault()
                     ?? l.Where(x => Grp(x) && x.Cat.ObraId is null).Cast<(CarteiraComprador, CarteiraCategoria)?>().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(grupo)) achado = null;
        }
        if (achado is not { } a) return new Destino(null, null, null, null, categoria);
        var titular = a.Item1;
        if (!titular.AusenteEm(hoje)) return new Destino(titular.UsuarioId, titular.Nome, titular.UsuarioId, titular.Nome, categoria);
        var sub = titular.SubstitutoUsuarioId is { } sid ? ativas.FirstOrDefault(c => c.UsuarioId == sid) : null;
        if (titular.SubstitutoUsuarioId is { } sid2 && (sub is null || !sub.AusenteEm(hoje)))
            return new Destino(sid2, sub?.Nome ?? titular.SubstitutoNome, titular.UsuarioId, titular.Nome, categoria);
        return new Destino(null, null, titular.UsuarioId, titular.Nome, categoria);
    }

    /// <summary>Duas carteiras com a mesma categoria e a mesma obra deixam o encaminhamento ambíguo: bloqueia no cadastro.</summary>
    public static IReadOnlyList<string> Conflitos(CarteiraComprador editada, IEnumerable<CarteiraComprador> outras)
    {
        string Chave(CarteiraCategoria c) => $"{K(c.Grupo)}|{K(c.SubGrupo)}|{K(c.CategoriaServico)}|{c.ObraId}";
        var e = new List<string>();
        foreach (var g in editada.Categorias.GroupBy(Chave).Where(g => g.Count() > 1)) e.Add($"Categoria repetida na carteira: {g.First().Rotulo}.");
        foreach (var o in outras.Where(o => o.Id != editada.Id && o.Ativa && !o.Excluido))
            foreach (var c in editada.Categorias)
                if (o.Categorias.Any(x => Chave(x) == Chave(c)))
                    e.Add($"\"{c.Rotulo}\"{(c.ObraId is null ? "" : " (nesta obra)")} já está na carteira de {o.Nome}. Escolha um dono só, ou limite por obra.");
        return e;
    }

    /// <summary>Data limite de compra do lote: menor (necessidade − lead time) entre os itens.</summary>
    public static DateOnly? DataLimite(IEnumerable<RequisicaoItem> itens, DateOnly dataEntrega) =>
        itens.Select(i => (i.DataNecessidade ?? dataEntrega).AddDays(-Math.Max(0, i.LeadTimeDias))).DefaultIfEmpty().Min() is var d && d == default ? null : d;

    public static string Letra(int indice) => indice < 26 ? ((char)('A' + indice)).ToString() : $"Z{indice - 25}";
}
