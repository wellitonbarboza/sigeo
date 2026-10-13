using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Domain.Regras;

/// <summary>Resultado de uma verificação de trava: lista de bloqueios legíveis (vazia = liberado).</summary>
public sealed record ResultadoTrava(IReadOnlyList<string> Bloqueios)
{
    public static ResultadoTrava Ok { get; } = new([]);
    public bool Liberado => Bloqueios.Count == 0;
    public string? Resumo => Liberado ? null : string.Join(" | ", Bloqueios);
}

public static class StatusDocumentoCalculo
{
    public static StatusDocumento Calcular(DateOnly? validade, bool temArquivo, int diasAlerta, DateOnly hoje)
    {
        if (!temArquivo) return StatusDocumento.PendenteEnvio;
        if (validade is null) return StatusDocumento.Valido;
        if (validade.Value < hoje) return StatusDocumento.Vencido;
        return validade.Value <= hoje.AddDays(diasAlerta) ? StatusDocumento.AVencer : StatusDocumento.Valido;
    }

    public static StatusDocumento Calcular(FornecedorDocumento d, DateOnly hoje) =>
        d.Status is StatusDocumento.Reprovado or StatusDocumento.EmValidacao ? d.Status
        : Calcular(d.DataValidade, d.ArquivoBlob is not null, d.TipoDocumento?.DiasAlertaAntesVencimento ?? 30, hoje);
}

/// <summary>
/// Travas de qualidade: amarram o SGQ ao fluxo de compras e contratos.
/// Regra nova entra aqui e ganha teste.
/// </summary>
public static class TravasQualidade
{
    /// <summary>Fornecedor apto a vender o escopo informado (materiais controlados / serviços controlados) na data.</summary>
    public static ResultadoTrava VerificarFornecedor(Fornecedor f, DateOnly hoje, IEnumerable<Guid?>? materiais = null, IEnumerable<Guid?>? servicos = null)
    {
        var b = new List<string>();
        if (!f.Ativo || f.StatusQualificacao == StatusQualificacao.Desqualificado)
            b.Add($"Fornecedor {f.NomeExibicao} desqualificado/inativo.");

        foreach (var m in (materiais ?? []).Where(m => m is not null).Distinct())
            if (f.QualificacaoVigente(hoje, materialId: m) is null)
                b.Add($"Fornecedor {f.NomeExibicao} sem qualificação vigente para o material controlado.");
        foreach (var s in (servicos ?? []).Where(s => s is not null).Distinct())
            if (f.QualificacaoVigente(hoje, servicoId: s) is null)
                b.Add($"Fornecedor {f.NomeExibicao} sem qualificação vigente para o serviço controlado.");

        if (f.Qualificacoes.Any(q => !q.Excluido && q.Psq == SituacaoPsq.NaoConforme && (q.DataValidade is null || q.DataValidade >= hoje)))
            b.Add($"Fornecedor {f.NomeExibicao} 'Não conforme' no PSQ: compra vedada.");

        b.AddRange(DocumentosBloqueantes(f, hoje));
        return new ResultadoTrava(b.Distinct().ToList());
    }

    /// <summary>Cotação → pedido: propostas mínimas, vencedor qualificado, especificação atendida nos itens controlados.</summary>
    public static ResultadoTrava VerificarCotacao(Cotacao c, ParametrosVigentes p, DateOnly hoje)
    {
        var b = new List<string>();
        c.Recalcular(p);
        if (c.PropostasRecebidas < c.PropostasExigidas && string.IsNullOrWhiteSpace(c.JustificativaPropostas))
            b.Add($"Faixa {c.FaixaAlcada(p)}: {c.PropostasExigidas} proposta(s) exigida(s), {c.PropostasRecebidas} recebida(s). Justifique (fornecedor exclusivo, emergência…).");
        if (c.FornecedorVencedor is null) b.Add("Escolha o fornecedor vencedor.");
        if (c.CriterioSelecao is null) b.Add("Informe o critério de seleção.");
        var itensVencedor = c.Itens.Where(i => i.FornecedorId == c.FornecedorVencedorId).ToList();
        foreach (var i in itensVencedor.Where(i => i.MaterialControlado && i.AtendeEspecificacao != true))
            b.Add($"{i.Descricao}: confirme que o ofertado atende à especificação técnica.");
        foreach (var i in itensVencedor)
            if (i.ErroMarca(c.Tipo == TipoRequisicao.Material, i.RequisicaoItem?.Marca, i.RequisicaoItem?.MesmaMarca == true) is { } em) b.Add(em);
        if (c.FornecedorVencedor is { } v)
        {
            var materiais = itensVencedor.Select(i => i.RequisicaoItem?.MaterialControladoId);
            var servicos = itensVencedor.Select(i => i.RequisicaoItem?.ServicoControladoId);
            b.AddRange(VerificarFornecedor(v, hoje, materiais, servicos).Bloqueios);
        }
        return new ResultadoTrava(b);
    }

    /// <summary>Pedido: fornecedor apto para os materiais controlados do pedido, e-mail para envio, especificação nos itens controlados.</summary>
    public static ResultadoTrava VerificarPedido(Pedido pedido, Fornecedor fornecedor, DateOnly hoje)
    {
        var b = new List<string>();
        var controlados = pedido.Itens.Where(i => i.MaterialControladoId is not null).ToList();
        b.AddRange(VerificarFornecedor(fornecedor, hoje, controlados.Select(i => i.MaterialControladoId)).Bloqueios);
        if (string.IsNullOrWhiteSpace(fornecedor.Emails)) b.Add("Fornecedor sem e-mail cadastrado (envio do pedido).");
        foreach (var i in controlados.Where(i => string.IsNullOrWhiteSpace(i.EspecificacaoTecnica)))
            b.Add($"{i.Descricao}: item controlado sem especificação técnica no pedido.");
        if (pedido.TipoDocumento == TipoDocumentoCompra.Contrato && pedido.ContratoId is null)
            b.Add("Tipo de documento 'Contrato': vincule o contrato.");
        return new ResultadoTrava(b);
    }

    /// <summary>Contrato: fornecedor qualificado para as ITS e documentos em dia.</summary>
    public static ResultadoTrava VerificarContrato(Contrato c, DateOnly hoje)
    {
        var b = new List<string>(c.Validar());
        var servicos = c.Itens.Select(i => i.ServicoControladoId).Concat(c.ServicosVinculados.Select(s => (Guid?)s.ServicoControladoId));
        b.AddRange(VerificarFornecedor(c.Fornecedor, hoje, servicos: servicos).Bloqueios);
        return new ResultadoTrava(b);
    }

    /// <summary>
    /// Medição: contrato ativo, acumulado ≤ contratado (no total e por local), FVS liberada para cada item de serviço controlado
    /// (no local medido — ou num nível acima — e em cada local do orçamento abaixo dele), avaliação preenchida,
    /// documentos do prestador em dia e documentos mensais da competência entregues até o dia limite.
    /// </summary>
    /// <param name="locais">Árvore de locais da obra (para aceitar FVS aberta num nível acima e exigir os locais abaixo).</param>
    /// <param name="locaisExigidos">Para cada item medido, os locais do orçamento (células) abaixo do local medido que precisam de FVS.</param>
    /// <param name="tiposDocumento">Matriz de documentos (para cobrar os mensais).</param>
    public static ResultadoTrava VerificarMedicao(Medicao m, IEnumerable<Fvs> fvsDaObra, DateOnly hoje, IReadOnlyDictionary<Guid, Local>? locais = null,
        Func<MedicaoItem, IReadOnlyCollection<Guid>>? locaisExigidos = null, IEnumerable<TipoDocumentoExigido>? tiposDocumento = null)
    {
        var b = new List<string>();
        if (m.Contrato.Status != StatusContrato.Ativo) b.Add("Contrato não está ativo.");
        var fvs = fvsDaObra.Where(f => f.Liberada).ToList();
        var liberadas = fvs.Select(f => (f.ServicoControladoId, f.LocalId)).ToHashSet();
        bool Liberado(Guid servico, Guid local)
        {
            Guid? x = local;
            while (x is { } id)
            {
                if (liberadas.Contains(((Guid?)servico, id))) return true;
                x = locais is not null && locais.TryGetValue(id, out var l) ? l.LocalPaiId : null;
            }
            return false;
        }
        // Total do item (somando todos os locais desta medição e das anteriores) nunca passa do contratado.
        foreach (var g in m.Itens.Where(i => i.ContratoItem.Locais.Count > 0).GroupBy(i => i.ContratoItem))
        {
            var porLocal = g.GroupBy(i => i.LocalId).Select(x => x.Max(i => i.QtdAcumulada)).Sum();
            if (porLocal > g.Key.Quantidade + 0.0001m)
                b.Add($"Item {g.Key.Item}: acumulado somando os locais ({porLocal}) maior que o contratado ({g.Key.Quantidade}) — exige aditivo.");
        }
        foreach (var i in m.Itens)
        {
            var ci = i.ContratoItem;
            var cl = i.LocalId is { } lid ? ci.Locais.FirstOrDefault(x => x.LocalId == lid) : null;
            if (cl is not null && i.QtdAcumulada > cl.Quantidade + 0.0001m)
                b.Add($"Item {ci.Item}: acumulado no local maior que o contratado para ele ({cl.Quantidade}) — exige aditivo.");
            else if (cl is null && ci.Locais.Count > 0 && i.LocalId is not null)
                b.Add($"Item {ci.Item}: local medido não faz parte do contrato — inclua por aditivo.");
            else if (cl is null && i.QtdAcumulada > ci.Quantidade + 0.0001m)
                b.Add($"Item {ci.Item}: acumulado ({i.QtdAcumulada}) maior que o contratado ({ci.Quantidade}) — exige aditivo.");
            if (ci.ServicoControlado is { ExigeFvsParaMedicao: true } sc && i.QtdPeriodo > 0)
            {
                bool ok;
                if (i.FvsId is { } fid) ok = fvs.Any(f => f.Id == fid);
                else if (i.LocalId is null) ok = fvs.Any(f => f.ServicoControladoId == sc.Id);
                else
                {
                    var exigidos = locaisExigidos?.Invoke(i) ?? [];
                    ok = exigidos.Count == 0 ? Liberado(sc.Id, i.LocalId.Value) || DescendenteLiberado(sc.Id, i.LocalId.Value)
                                             : exigidos.All(l => Liberado(sc.Id, l));
                }
                if (!ok) b.Add($"Item {ci.Item} ({sc.Codigo} – {sc.Nome}): sem FVS liberada{(i.LocalId is null ? "" : " em todos os locais medidos")}.");
            }
        }
        bool DescendenteLiberado(Guid servico, Guid local) =>
            locais is not null && fvs.Any(f => f.ServicoControladoId == servico && EhDescendente(f.LocalId, local));
        bool EhDescendente(Guid filho, Guid pai)
        {
            Guid? x = filho;
            while (x is { } id && locais!.TryGetValue(id, out var l)) { if (l.LocalPaiId == pai) return true; x = l.LocalPaiId; }
            return false;
        }
        if (m.NotaFinal is null) b.Add("Preencha a avaliação do prestador (notas obrigatórias).");
        if (m.Faixa == FaixaNota.ReuniaoPlanoCompleto && m.DataReuniao is null) b.Add("Nota abaixo do mínimo: registre a reunião com o fornecedor.");
        b.AddRange(DocumentosBloqueantes(m.Contrato.Fornecedor, hoje));
        if (tiposDocumento is not null) b.AddRange(DocumentosMensaisPendentes(m.Contrato.Fornecedor, tiposDocumento, hoje));
        return new ResultadoTrava(b.Distinct().ToList());
    }

    /// <summary>
    /// Documentos mensais (eSocial/DCTFWeb, guias INSS/FGTS, folha, seguro de vida…): a competência anterior precisa estar
    /// entregue (e não recusada) a partir do dia limite (padrão: dia 20). Vale para os documentos exigidos deste fornecedor.
    /// </summary>
    public static IEnumerable<string> DocumentosMensaisPendentes(Fornecedor f, IEnumerable<TipoDocumentoExigido> tipos, DateOnly hoje)
    {
        var exigidos = f.Exigencias.Count > 0 ? f.Exigencias.Where(x => x.Obrigatorio).Select(x => x.TipoDocumentoExigidoId).ToHashSet() : null;
        foreach (var t in tipos.Where(t => t.Frequencia == FrequenciaDocumento.Mensal && t.Ativo && t.BloqueiaQuandoVencido))
        {
            var aplica = exigidos is not null ? exigidos.Contains(t.Id) : MatrizExigencias.Aplica(t, f.TipoFornecimento, f.IncluirDocsFuncionarios) && t.Obrigatorio;
            var comp = MatrizExigencias.CompetenciaExigida(t, hoje);
            if (!aplica || comp is null) continue;
            var ok = f.Documentos.Any(d => !d.Excluido && d.TipoDocumentoExigidoId == t.Id && d.Competencia == comp && d.ArquivoBlob is not null && d.Status != StatusDocumento.Reprovado);
            if (!ok) yield return $"Documento mensal '{t.Nome}' da competência {comp:MM/yyyy} não entregue (limite: dia {t.DiaLimite ?? 20}).";
        }
    }

    /// <summary>
    /// Pagamento (3-way): material → pedido × recebimento com FVM aprovada (ou tratada) × NF;
    /// serviço → medição aprovada. Diferença entre NF e liberado exige justificativa.
    /// </summary>
    public static ResultadoTrava VerificarPagamento(LiberacaoPagamento l, Pedido? pedido, IEnumerable<Recebimento> recebimentos, Medicao? medicao)
    {
        var b = new List<string>();
        if (l.ChaveNFe is not { Length: 44 } || !l.ChaveNFe.All(char.IsDigit)) b.Add("Informe a chave da NF-e (44 dígitos).");
        if (l.ValorLiberado != l.ValorNF && string.IsNullOrWhiteSpace(l.JustificativaDiferenca))
            b.Add("Valor liberado diferente do valor da NF: justifique.");
        if (l.Origem == OrigemPagamento.NotaMaterial)
        {
            if (pedido is null) b.Add("Vincule o pedido de compra.");
            var recs = recebimentos.Where(r => !r.Excluido && r.PedidoId == l.PedidoId).ToList();
            if (recs.Count == 0) b.Add("Sem recebimento (FVM) registrado para o pedido.");
            else if (recs.Any(r => r.FvmAprovada == false && r.Destino == DestinoRecebimento.Devolvido))
                b.Add("Recebimento com FVM reprovada e material devolvido: pagamento bloqueado.");
            else if (recs.Any(r => r.FvmAprovada == false && string.IsNullOrWhiteSpace(r.Ocorrencia)))
                b.Add("FVM reprovada sem tratativa registrada.");
            l.RecebimentoOk = b.Count == 0;
        }
        else
        {
            if (medicao is null) b.Add("Vincule a medição.");
            else if (medicao.Status is not (StatusMedicao.Aprovada or StatusMedicao.NfRecebida)) b.Add("Medição ainda não aprovada pelo Diretor Técnico.");
            l.RecebimentoOk = medicao?.Status is StatusMedicao.Aprovada or StatusMedicao.NfRecebida;
        }
        return new ResultadoTrava(b);
    }

    public static IEnumerable<string> DocumentosBloqueantes(Fornecedor f, DateOnly hoje) =>
        f.Documentos
         .Where(d => !d.Excluido && d.Bloqueia && d.TipoDocumento is { PorEntrega: false } && d.TipoDocumento.Frequencia != FrequenciaDocumento.Mensal)
         .GroupBy(d => d.TipoDocumentoExigidoId)
         .Select(g => g.OrderByDescending(d => d.DataValidade ?? DateOnly.MaxValue).First())
         .Where(d => StatusDocumentoCalculo.Calcular(d, hoje) is StatusDocumento.Vencido or StatusDocumento.PendenteEnvio or StatusDocumento.Reprovado)
         .Select(d => $"Documento do fornecedor '{d.TipoDocumento.Nome}' vencido, pendente ou recusado.");
}

/// <summary>Planos de ação automáticos (P.SUP 3.3): nota de medição &lt; 9 e FVM reprovada.</summary>
public static class RegrasAvaliacao
{
    public static PlanoAcaoFornecedor? PlanoPorMedicao(Medicao m, Guid fornecedorId, DateOnly hoje, string numero)
    {
        if (m.NotaFinal is null || m.Faixa == FaixaNota.Nenhuma) return null;
        var piores = new (string Nome, decimal? Nota)[]
        {
            ("cumprimento do escopo", m.NotaEscopo), ("qualidade do material", m.NotaMaterial), ("qualidade do serviço", m.NotaServico),
            ("limpeza e organização", m.NotaLimpeza), ("documentação e segurança do trabalho", m.NotaDocSeg)
        }.Where(c => c.Nota is not null).OrderBy(c => c.Nota).Take(2).Select(c => $"{c.Nome} (nota {c.Nota:0.0})");
        return new PlanoAcaoFornecedor
        {
            EmpresaId = m.EmpresaId, Numero = numero, FornecedorId = fornecedorId, ObraId = m.ObraId,
            Origem = OrigemPlanoAcao.Medicao, OrigemId = m.Id,
            TipoPlano = m.Faixa == FaixaNota.ReuniaoPlanoCompleto ? TipoPlanoAcao.Completo : TipoPlanoAcao.Pontual,
            Problema = $"Medição {m.NumeroMedicao:00} com nota {m.NotaFinal:0.00}: {string.Join("; ", piores)}.",
            Acao = "A definir com o fornecedor.", Responsavel = m.AvaliadorNome ?? "Engenheiro da obra",
            Prazo = hoje.AddDays(m.Faixa == FaixaNota.ReuniaoPlanoCompleto ? 15 : 30)
        };
    }

    public static PlanoAcaoFornecedor? PlanoPorFvm(Recebimento r, DateOnly hoje, string numero) =>
        r.FvmAprovada != false ? null : new PlanoAcaoFornecedor
        {
            EmpresaId = r.EmpresaId, Numero = numero, FornecedorId = r.FornecedorId, ObraId = r.ObraId,
            Origem = OrigemPlanoAcao.Fvm, OrigemId = r.Id, TipoPlano = TipoPlanoAcao.Pontual,
            Problema = $"FVM reprovada no recebimento {r.Numero}: {r.Ocorrencia}", Acao = "A definir com o fornecedor.",
            Responsavel = "Suprimentos", Prazo = hoje.AddDays(30)
        };
}
