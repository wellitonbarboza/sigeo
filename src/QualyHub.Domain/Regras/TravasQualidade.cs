using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Medicoes;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Suprimentos;

namespace QualyHub.Domain.Regras;

/// <summary>Resultado de uma verificação de trava: lista de bloqueios legíveis (vazia = liberado).</summary>
public sealed record ResultadoTrava(IReadOnlyList<string> Bloqueios)
{
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
}

/// <summary>
/// Travas entre ERP (Obra Prima) e SGQ. É aqui que o QualyHub "amarra" qualidade ao fluxo operacional,
/// e é essa amarração que gera a evidência pedida na auditoria do SiAC.
/// </summary>
public static class TravasQualidade
{
    /// <summary>Pedido: fornecedor de material controlado precisa estar qualificado e sem documento bloqueante vencido.</summary>
    public static ResultadoTrava VerificarPedido(Pedido pedido, Fornecedor fornecedor, DateOnly hoje)
    {
        var bloqueios = new List<string>();
        var itensControlados = pedido.Itens.Where(i => i.MaterialControlado is { ExigeFornecedorQualificado: true }).ToList();

        if (itensControlados.Count > 0 &&
            fornecedor.Situacao is not (SituacaoFornecedor.Qualificado or SituacaoFornecedor.QualificadoComRestricao))
        {
            bloqueios.Add($"Fornecedor {fornecedor.RazaoSocial} não qualificado para material controlado " +
                          $"({string.Join(", ", itensControlados.Select(i => i.MaterialControlado!.Nome).Distinct())}).");
        }

        if (fornecedor.ProximaReavaliacao is { } reav && reav < hoje && itensControlados.Count > 0)
            bloqueios.Add($"Reavaliação do fornecedor vencida desde {reav:dd/MM/yyyy}.");

        bloqueios.AddRange(DocumentosBloqueantes(fornecedor, hoje));
        return new ResultadoTrava(bloqueios);
    }

    /// <summary>
    /// Medição: cada item de serviço controlado precisa de FVS liberada para o mesmo serviço
    /// (e o mesmo local, quando o local foi informado no export).
    /// </summary>
    public static ResultadoTrava VerificarMedicao(Medicao medicao, IEnumerable<Ficha> fvsDaObra, Fornecedor fornecedor, DateOnly hoje)
    {
        var bloqueios = new List<string>();
        var fvsLiberadas = fvsDaObra.Where(f => f.Tipo == TipoFicha.FVS && f.Liberada).ToList();

        foreach (var item in medicao.Itens.Where(i => i.ServicoControlado is { ExigeFvsParaMedicao: true }))
        {
            var temFvs = fvsLiberadas.Any(f =>
                f.ServicoControladoId == item.ServicoControladoId &&
                (item.LocalId is null || f.LocalId == item.LocalId));

            if (!temFvs)
                bloqueios.Add($"Item {item.CodigoItem} ({item.ServicoControlado!.Nome}{(item.LocalTexto is null ? "" : $" – {item.LocalTexto}")}): sem FVS aprovada.");
        }

        bloqueios.AddRange(DocumentosBloqueantes(fornecedor, hoje));
        return new ResultadoTrava(bloqueios);
    }

    /// <summary>FVM: aceite de material controlado só com ficha liberada; quantidade aceita alimenta o item do pedido.</summary>
    public static ResultadoTrava VerificarRecebimento(PedidoItem item, IEnumerable<Ficha> fvmDoItem)
    {
        if (item.MaterialControlado is not { ExigeFvmParaAceite: true }) return new ResultadoTrava([]);
        var aceita = fvmDoItem.Where(f => f.Tipo == TipoFicha.FVM && f.Liberada).Sum(f => f.QuantidadeAceita ?? 0);
        return aceita >= item.Quantidade
            ? new ResultadoTrava([])
            : new ResultadoTrava([$"Material {item.Descricao}: aceito {aceita} de {item.Quantidade} {item.Unidade} via FVM."]);
    }

    /// <summary>Funcionário: acesso à obra somente com documentos obrigatórios válidos.</summary>
    public static ResultadoTrava VerificarAcessoFuncionario(Funcionario funcionario, IEnumerable<TipoDocumentoExigido> exigidos, DateOnly hoje)
    {
        var bloqueios = new List<string>();
        foreach (var tipo in exigidos.Where(t => t is { AplicaA: AplicaA.Funcionario, Obrigatorio: true, Ativo: true }))
        {
            var doc = funcionario.Documentos.Where(d => d.TipoDocumentoExigidoId == tipo.Id && !d.Excluido)
                                            .OrderByDescending(d => d.DataValidade).FirstOrDefault();
            if (doc is null) { bloqueios.Add($"{tipo.Nome}: não apresentado."); continue; }
            if (doc.Apto == false) bloqueios.Add($"{tipo.Nome}: inapto.");
            var status = StatusDocumentoCalculo.Calcular(doc.DataValidade, doc.ArquivoBlob is not null, tipo.DiasAlertaAntesVencimento, hoje);
            if (status is StatusDocumento.Vencido or StatusDocumento.PendenteEnvio && tipo.BloqueiaQuandoVencido)
                bloqueios.Add($"{tipo.Nome}: {(status == StatusDocumento.Vencido ? "vencido" : "arquivo pendente")}.");
        }
        return new ResultadoTrava(bloqueios);
    }

    private static IEnumerable<string> DocumentosBloqueantes(Fornecedor fornecedor, DateOnly hoje) =>
        fornecedor.Documentos
            .Where(d => !d.Excluido && d.TipoDocumento is { BloqueiaQuandoVencido: true })
            .Where(d => StatusDocumentoCalculo.Calcular(d.DataValidade, d.ArquivoBlob is not null, d.TipoDocumento.DiasAlertaAntesVencimento, hoje)
                        is StatusDocumento.Vencido or StatusDocumento.PendenteEnvio)
            .Select(d => $"Documento do fornecedor '{d.TipoDocumento.Nome}' vencido ou pendente.");
}
