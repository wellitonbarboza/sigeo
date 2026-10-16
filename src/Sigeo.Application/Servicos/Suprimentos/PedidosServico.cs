using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Engenharia;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;
using Sigeo.Domain.Qualidade;

namespace Sigeo.Application.Servicos;

/// <summary>Pedido de compra (T07): aprovação do Diretor e envio ao fornecedor.</summary>
public sealed class PedidosServico(IFabricaDb fabrica, AprovacaoServico aprovacoes, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ───────── T07 Pedido ─────────

    public async Task EnviarPedidoAprovacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.Pedidos.Include(x => x.Itens)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Qualificacoes)
            .Include(x => x.Fornecedor).ThenInclude(f => f.Documentos).ThenInclude(d => d.TipoDocumento)
            .FirstAsync(x => x.Id == id, ct);
        var trava = TravasQualidade.VerificarPedido(p, p.Fornecedor, Hoje);
        p.Bloqueios = trava.Resumo;
        if (!trava.Liberado) { await db.SaveChangesAsync(ct); ValidacaoException.Se(trava.Bloqueios); }
        await aprovacoes.EnviarAsync(db, EntidadeAprovavel.Pedido, p.Id, p.ObraId, $"Pedido {p.Numero} – {p.Fornecedor.NomeExibicao}", p.Valor, ct, p.SpeId);
        p.Status = StatusPedido.AguardandoAprovacaoDiretor;
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEnvioPedidoAsync(Guid id, string emailsCopia, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.Pedidos.FirstAsync(x => x.Id == id, ct);
        if (p.Status != StatusPedido.Aprovado) throw new RegraDeNegocioException("Pedido precisa estar aprovado pelo Diretor antes do envio.");
        p.EnviadoFornecedorEm = relogio.GetUtcNow();
        p.EmailsCopia = emailsCopia;
        p.Status = StatusPedido.EnviadoFornecedor;
        await db.SaveChangesAsync(ct);
    }
}
