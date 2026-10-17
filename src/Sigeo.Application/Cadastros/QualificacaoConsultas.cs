using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Cadastros;

/// <summary>Leituras da tela de qualificação de fornecedores (Cadastros/Qualificacao).</summary>
public sealed class QualificacaoConsultas(IFabricaDb fabrica)
{
    public async Task<ParametrosVigentes> ParametrosAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await ParametrosServico.CarregarAsync(db, ct: ct);
    }

    /// <summary>Qualificações, mais recentes primeiro; com fornecedor, só as dele.</summary>
    public async Task<List<QualificacaoLinha>> ListarAsync(Guid? fornecedor, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var q = db.Qualificacoes.AsNoTracking();
        if (fornecedor is { } f) q = q.Where(x => x.FornecedorId == f);
        return await q.OrderByDescending(x => x.DataQualificacao)
            .Select(x => new QualificacaoLinha(x.Id, x.Fornecedor.RazaoSocial, x.TipoFornecimento,
                x.MaterialControlado != null ? x.MaterialControlado.Nome : x.ServicoControlado != null ? x.ServicoControlado.Nome : null,
                x.CriterioQualificacao, x.Psq, x.DataQualificacao, x.DataValidade, x.Resultado, x.ResponsavelNome, x.CriadoPor)).ToListAsync(ct);
    }

    public async Task<FornecedorQualificacao> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.Qualificacoes.AsNoTracking().FirstAsync(x => x.Id == id, ct);
    }
}
