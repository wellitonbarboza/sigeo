using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Groundkey.Application.Abstracoes;
using Groundkey.Application.Comum;
using Groundkey.Domain.Aprovacoes;

namespace Groundkey.Infrastructure.Servicos;

/// <summary>
/// Exclusão genérica (ver <see cref="IExclusao"/>). As referências vêm do modelo do EF: toda chave estrangeira que aponta
/// para a entidade e não é parte do mesmo agregado (exclusão em cascata) conta como uso.
/// </summary>
public sealed class Exclusao(IFabricaDb fabrica, IUsuarioAtual usuario) : IExclusao
{
    private static readonly Dictionary<string, string> Nomes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RequisicaoItem"] = "itens de requisição", ["PedidoItem"] = "itens de pedido", ["CotacaoItem"] = "itens de cotação", ["ContratoItem"] = "itens de contrato",
        ["Produto"] = "produtos", ["MovimentoEstoque"] = "movimentos de estoque", ["SaldoEstoque"] = "saldos de estoque", ["KitMaterialItem"] = "kits",
        ["Equipamento"] = "equipamentos", ["OrdemServico"] = "ordens de serviço", ["PlanoManutencao"] = "planos de manutenção", ["Local"] = "locais",
        ["Requisicao"] = "requisições", ["Pedido"] = "pedidos", ["Contrato"] = "contratos", ["Medicao"] = "medições", ["Recebimento"] = "recebimentos",
        ["RecebimentoItem"] = "itens de recebimento", ["ComposicaoItem"] = "composições", ["ServicoLocal"] = "orçamento por local", ["Colaborador"] = "colaboradores",
        ["Fvs"] = "FVS", ["Rnc"] = "RNCs", ["Usuario"] = "usuários", ["Obra"] = "obras", ["Spe"] = "SPEs", ["Estabelecimento"] = "estabelecimentos",
        ["EquipamentoMedicao"] = "equipamentos de medição", ["TransferenciaItem"] = "itens de transferência", ["Cotacao"] = "cotações",
        ["CotacaoFornecedor"] = "cotações (fornecedor convidado)", ["DocumentoFornecedor"] = "documentos de fornecedor", ["NotaFiscalEntrada"] = "notas fiscais",
    };

    private static string Nome(IEntityType t) => Nomes.TryGetValue(t.ClrType.Name, out var n) ? n
        : System.Text.RegularExpressions.Regex.Replace(t.ClrType.Name, "(?<!^)([A-Z])", " $1").ToLowerInvariant();

    public async Task<IReadOnlyList<string>> ImpedimentosAsync(Type tipo, Guid id, CancellationToken ct = default)
    {
        await using var db = (DbContext)fabrica.CriarLeitura();
        return await ImpedimentosAsync(db, tipo, id, ct);
    }

    private static async Task<List<string>> ImpedimentosAsync(DbContext db, Type tipo, Guid id, CancellationToken ct)
    {
        var et = db.Model.FindEntityType(tipo) ?? throw new RegraDeNegocioException("Tipo de registro desconhecido.");
        var e = new List<string>();
        var ent = await db.FindAsync(tipo, [id], ct) ?? throw new RegraDeNegocioException("Registro não encontrado (já excluído?).");
        if (RegrasExclusao.Validar(ent) is { } regra) e.Add(regra);
        foreach (var fk in et.GetReferencingForeignKeys())
        {
            if (fk.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.ClientCascade) continue; // parte do mesmo agregado
            var dep = fk.DeclaringEntityType;
            if (dep.IsOwned() || dep.GetTableName() is not { } tabela || fk.Properties.Count != 1) continue;
            var col = fk.Properties[0].GetColumnName(StoreObjectIdentifier.Table(tabela, dep.GetSchema()));
            var excl = dep.FindProperty("Excluido") is not null ? " AND [Excluido] = 0" : "";
            var sql = $"SELECT COUNT(*) AS [Value] FROM [{dep.GetSchema() ?? "dbo"}].[{tabela}] WHERE [{col}] = {{0}}{excl}";
            var n = await db.Database.SqlQueryRaw<int>(sql, id).SingleAsync(ct);
            if (n > 0) e.Add($"em uso em {n} {Nome(dep)}");
        }
        return e.Distinct().ToList();
    }

    public async Task ExcluirAsync(Type tipo, Guid id, string? rota, CancellationToken ct = default)
    {
        await using var q = fabrica.Criar();
        if (!string.IsNullOrWhiteSpace(rota)) await Permissoes.ExigirTelaAsync(q, usuario, rota, 'E', ct);
        var db = (DbContext)q;
        var imp = await ImpedimentosAsync(db, tipo, id, ct);
        if (imp.Count > 0)
            throw new RegraDeNegocioException("Não pode excluir: " + string.Join("; ", imp) + ". Inative o cadastro para deixar de usar.");
        var ent = await db.FindAsync(tipo, [id], ct);
        db.Remove(ent!);
        await db.SaveChangesAsync(ct);
    }
}
