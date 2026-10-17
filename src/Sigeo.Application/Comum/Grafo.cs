using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Application.Comum;

/// <summary>
/// Salva um agregado editado fora do contexto (tela): carrega o atual, copia os valores simples e sincroniza as coleções
/// (inclui novos, atualiza existentes, exclui os removidos). Referências de navegação dos filhos são ignoradas (só as FKs contam).
/// </summary>
public static class Grafo
{
    public static async Task<T> SalvarAsync<T>(ISigeoDb db, T novo, params string[] colecoes) where T : Entidade
    {
        IQueryable<T> q = db.Set<T>();
        foreach (var c in colecoes) q = q.Include(c);
        var atual = await q.FirstOrDefaultAsync(e => e.Id == novo.Id);
        if (atual is null)
        {
            foreach (var c in colecoes) foreach (var f in Filhos(novo, c)) LimparReferencias(db, f);
            LimparReferencias(db, novo);
            db.Add(novo);
            return novo;
        }

        var entry = db.Entry(atual);
        var rowVersion = entry.Metadata.FindProperty(nameof(EntidadeAuditavel.RowVersion));
        entry.CurrentValues.SetValues(novo);
        if (rowVersion is not null) entry.Property(rowVersion.Name).OriginalValue = db.Entry(novo).Property(rowVersion.Name).CurrentValue;

        var filhosMudaram = false;
        foreach (var c in colecoes)
        {
            var colAtual = entry.Collection(c);
            var itensAtuais = ((System.Collections.IEnumerable)colAtual.CurrentValue!).Cast<Entidade>().ToList();
            var itensNovos = Filhos(novo, c).ToList();
            var lista = colAtual.CurrentValue!;
            var add = lista.GetType().GetMethod("Add")!;
            var novosPorId = itensNovos.ToDictionary(n => n.Id);
            foreach (var a in itensAtuais.Where(a => !novosPorId.ContainsKey(a.Id)))
            { db.Remove(a); filhosMudaram = true; }
            var atuaisPorId = itensAtuais.ToDictionary(a => a.Id);
            foreach (var n in itensNovos)
            {
                if (atuaisPorId.TryGetValue(n.Id, out var existente))
                {
                    var e = db.Entry(existente);
                    e.CurrentValues.SetValues(n);
                    filhosMudaram |= e.State == EntityState.Modified;
                }
                else { LimparReferencias(db, n); add.Invoke(lista, [n]); db.Entry(n).State = EntityState.Added; filhosMudaram = true; }
            }
        }
        // Só os itens mudaram: marca o agregado como alterado para o RowVersion ser conferido (dois usuários editando os itens
        // do mesmo documento não se sobrescrevem em silêncio) e a auditoria registrar quem alterou.
        if (filhosMudaram && rowVersion is not null && entry.State == EntityState.Unchanged) entry.State = EntityState.Modified;
        return atual;
    }

    private static IEnumerable<Entidade> Filhos(object pai, string colecao) =>
        ((System.Collections.IEnumerable?)pai.GetType().GetProperty(colecao)!.GetValue(pai) ?? Array.Empty<Entidade>()).Cast<Entidade>();

    /// <summary>Evita que objetos de referência (Produto, Fornecedor…) carregados na tela sejam inseridos de novo.</summary>
    private static void LimparReferencias(ISigeoDb db, object entidade)
    {
        var tipo = db.Model.FindEntityType(entidade.GetType());
        if (tipo is null) return;
        foreach (var nav in tipo.GetNavigations().Where(n => !n.IsCollection && n.IsOnDependent))
            if (nav.PropertyInfo?.CanWrite == true) nav.PropertyInfo.SetValue(entidade, null);
    }
}
