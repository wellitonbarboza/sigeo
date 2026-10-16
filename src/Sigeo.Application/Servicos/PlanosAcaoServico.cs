using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Servicos;

/// <summary>Planos de ação de fornecedores abertos/editados manualmente (os automáticos nascem em Suprimentos).</summary>
public sealed class PlanosAcaoServico(IFabricaDb fabrica)
{
    /// <summary>Cria (numerando PA-nnnn) ou atualiza o plano; fornecedor e origem não mudam depois de numerado.</summary>
    public Task<Guid> SalvarAsync(PlanoAcaoFornecedor p, CancellationToken ct = default) => GravarAsync(p, null, ct);

    /// <summary>Conclusão exige verificação de eficácia (regra no domínio); grava junto as últimas edições da tela.</summary>
    public Task<Guid> ConcluirAsync(PlanoAcaoFornecedor p, Eficacia eficacia, string? comentario, CancellationToken ct = default) =>
        GravarAsync(p, a => a.Concluir(eficacia, comentario), ct);

    public static IReadOnlyList<string> Validar(PlanoAcaoFornecedor p)
    {
        var e = new List<string>();
        if (p.FornecedorId == Guid.Empty) e.Add("Informe o fornecedor.");
        if (string.IsNullOrWhiteSpace(p.Problema)) e.Add("Descreva o problema.");
        if (string.IsNullOrWhiteSpace(p.Acao)) e.Add("Descreva a ação.");
        if (string.IsNullOrWhiteSpace(p.Responsavel)) e.Add("Informe o responsável.");
        return e;
    }

    private async Task<Guid> GravarAsync(PlanoAcaoFornecedor p, Action<PlanoAcaoFornecedor>? antes, CancellationToken ct)
    {
        ValidacaoException.Se(Validar(p));
        await using var db = fabrica.Criar();
        var atual = await db.PlanosAcao.FirstOrDefaultAsync(x => x.Id == p.Id, ct);
        if (atual is null)
        {
            // Mesma chave/formato de numeração dos planos gerados automaticamente em Suprimentos: a sequência é única.
            atual = new PlanoAcaoFornecedor
            {
                Id = p.Id, FornecedorId = p.FornecedorId, Origem = p.Origem, TipoPlano = p.TipoPlano, Problema = p.Problema, Acao = p.Acao,
                Responsavel = p.Responsavel, Prazo = p.Prazo, Status = p.Status,
                Numero = $"PA-{await Numerador.ProximoAsync(db, "PlanoAcao", ct):0000}"
            };
            db.PlanosAcao.Add(atual);
        }
        else
        {
            if (atual.Status == StatusPlanoAcao.Concluida && atual.Eficacia != Eficacia.NaoVerificada)
                throw new Domain.Aprovacoes.RegraDeNegocioException($"Plano {atual.Numero} já foi concluído.");
            atual.TipoPlano = p.TipoPlano; atual.Problema = p.Problema; atual.Acao = p.Acao; atual.Responsavel = p.Responsavel; atual.Prazo = p.Prazo; atual.Status = p.Status;
        }
        antes?.Invoke(atual);
        await db.SaveChangesAsync(ct);
        return atual.Id;
    }
}
