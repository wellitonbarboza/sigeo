using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Colaboracao;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Servicos;

/// <summary>Notas por registro e tarefas internas (prazo, prorrogação justificada, confirmação de leitura).</summary>
public sealed class ColaboracaoServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    // ---------------- Notas ----------------
    public async Task<List<Nota>> NotasAsync(string entidade, Guid entidadeId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        return await db.Notas.AsNoTracking().Where(n => n.Entidade == entidade && n.EntidadeId == entidadeId)
                             .OrderByDescending(n => n.CriadoEm).ToListAsync(ct);
    }

    public async Task AdicionarNotaAsync(string entidade, Guid entidadeId, string texto, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        db.Notas.Add(Nota.Criar(entidade, entidadeId, texto));
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Tarefas ----------------
    public async Task<Tarefa> CarregarTarefaAsync(Guid id, bool registrarLeitura, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tarefas.Include(x => x.Prorrogacoes).Include(x => x.Leituras).FirstAsync(x => x.Id == id, ct);
        if (registrarLeitura && usuario.UsuarioId is { } uid && t.ResponsavelId == uid)
        {
            if (t.RegistrarLeitura(uid, usuario.Nome, relogio.GetUtcNow()) is { } l) db.TarefaLeituras.Add(l);
            t.Iniciar();
            await db.SaveChangesAsync(ct);
        }
        return t;
    }

    public async Task<Guid> SalvarTarefaAsync(Tarefa t, CancellationToken ct = default)
    {
        ValidacaoException.Se(t.Validar(Hoje));
        await using var db = fabrica.Criar();
        var resp = await db.Usuarios.AsNoTracking().Where(u => u.Id == t.ResponsavelId).Select(u => u.Nome).FirstOrDefaultAsync(ct)
                   ?? throw new RegraDeNegocioException("Responsável não encontrado.");
        t.ResponsavelNome = resp;
        if (t.CriadoEm == default)
        {
            t.Numero = await Numerador.ProximoAsync(db, "Tarefa", ct);
            t.PrepararNova();
            db.Tarefas.Add(t);
        }
        else
        {
            var atual = await db.Tarefas.FirstAsync(x => x.Id == t.Id, ct);
            if (atual.Encerrada) throw new RegraDeNegocioException("Tarefa encerrada não pode ser alterada.");
            if (atual.Prazo != t.Prazo) throw new RegraDeNegocioException("Para mudar o prazo use \"Prorrogar\" (com justificativa).");
            atual.Titulo = t.Titulo; atual.Descricao = t.Descricao; atual.Tipo = t.Tipo; atual.Prioridade = t.Prioridade;
            atual.ObraId = t.ObraId; atual.ResponsavelId = t.ResponsavelId; atual.ResponsavelNome = t.ResponsavelNome;
            atual.MetaId = t.MetaId; atual.EquipeId = t.EquipeId;
        }
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    public async Task ProrrogarAsync(Guid id, DateOnly novoPrazo, string justificativa, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tarefas.Include(x => x.Prorrogacoes).FirstAsync(x => x.Id == id, ct);
        var p = t.Prorrogar(novoPrazo, justificativa, usuario.Nome);
        p.Em = relogio.GetUtcNow();
        db.TarefaProrrogacoes.Add(p);
        await db.SaveChangesAsync(ct);
    }

    public async Task ConcluirAsync(Guid id, string resolucao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tarefas.FirstAsync(x => x.Id == id, ct);
        t.Concluir(resolucao, usuario.Nome, relogio.GetUtcNow());
        if (t.MetaId is not null) t.Aceite = Domain.Colaboracao.AceiteTarefa.Aguardando; // entrega de meta: o gestor aceita ou devolve
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelarAsync(Guid id, string motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var t = await db.Tarefas.FirstAsync(x => x.Id == id, ct);
        t.Cancelar(motivo, usuario.Nome, relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }
}
