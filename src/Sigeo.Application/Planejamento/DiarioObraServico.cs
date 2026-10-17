using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Planejamento;

namespace Sigeo.Application.Planejamento;

/// <summary>
/// Diário de obra (RDO): um por obra e dia. Preenche a Engenharia; assina o engenheiro(a) da obra (o da obra ou de uma SPE dela;
/// sem engenheiro cadastrado, qualquer usuário da Engenharia). Assinado, não muda mais.
/// </summary>
public sealed class DiarioObraServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    public const string EntidadeAtividade = "DiarioAtividade";
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    public sealed record DiaMes(DateOnly Data, StatusDiario Status, DiaTrabalhado Trabalhado, bool Chuva);

    public async Task<List<DiaMes>> MesAsync(Guid obraId, int ano, int mes, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var ini = new DateOnly(ano, mes, 1); var fim = ini.AddMonths(1);
        return await db.DiariosObra.AsNoTracking().Where(d => d.ObraId == obraId && d.Data >= ini && d.Data < fim)
            .Select(d => new DiaMes(d.Data, d.Status, d.Trabalhado, d.ClimaManha >= ClimaDia.ChuvaFraca || d.ClimaTarde >= ClimaDia.ChuvaFraca)).ToListAsync(ct);
    }

    /// <summary>Diário do dia (o gravado ou um novo, ainda não salvo).</summary>
    public async Task<DiarioObra> AbrirAsync(Guid obraId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await db.DiariosObra.AsNoTracking().Include(d => d.Efetivo).Include(d => d.Atividades).Include(d => d.EquipamentosDia).AsSplitQuery().FirstOrDefaultAsync(d => d.ObraId == obraId && d.Data == data, ct)
               ?? new DiarioObra { ObraId = obraId, Data = data, HoraInicio = new TimeOnly(7, 0), HoraTermino = new TimeOnly(17, 0) };
    }

    /// <summary>Efetivo do último diário anterior (para copiar).</summary>
    public async Task<List<DiarioEfetivo>> EfetivoAnteriorAsync(Guid obraId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var d = await db.DiariosObra.AsNoTracking().Include(x => x.Efetivo).Where(x => x.ObraId == obraId && x.Data < data).OrderByDescending(x => x.Data).FirstOrDefaultAsync(ct);
        return d?.Efetivo.Select(e => new DiarioEfetivo { Terceiro = e.Terceiro, FuncaoObraId = e.FuncaoObraId, FornecedorId = e.FornecedorId, Nome = e.Nome, Quantidade = e.Quantidade }).ToList() ?? [];
    }

    /// <summary>Compromissos do plano semanal da semana do dia (para trazer como atividades).</summary>
    public async Task<List<DiarioAtividade>> AtividadesDoPlanoAsync(Guid obraId, DateOnly data, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        var seg = MedicaoSemanal.SegundaDe(data);
        var plano = await db.PlanosSemanais.AsNoTracking().Include(p => p.Compromissos).FirstOrDefaultAsync(p => p.ObraId == obraId && p.Semana == seg, ct);
        return plano?.Compromissos.OrderBy(c => c.Ordem).Select(c => new DiarioAtividade { Descricao = c.Descricao, AtividadeChave = c.AtividadeChave }).ToList() ?? [];
    }

    public async Task<Guid> SalvarAsync(DiarioObra d, CancellationToken ct = default)
    {
        foreach (var e in d.Efetivo) e.Nome = e.Nome?.Trim() ?? "";
        d.Efetivo = d.Efetivo.Where(e => e.Nome.Length > 0 || e.Quantidade > 0).ToList();
        d.Atividades = d.Atividades.Where(a => !string.IsNullOrWhiteSpace(a.Descricao) || a.LocalId != null).ToList();
        var ordem = 1;
        foreach (var a in d.Atividades) { a.Ordem = ordem++; a.Descricao = a.Descricao?.Trim() ?? ""; }
        ValidacaoException.Se(d.Validar(Hoje));
        await using var db = fabrica.Criar();
        await Permissoes.ExigirTelaAsync(db, usuario, "engenharia/diario", Contratos.AcaoTela.Incluir, ct);
        var atual = await db.DiariosObra.Include(x => x.Efetivo).Include(x => x.Atividades).Include(x => x.EquipamentosDia).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == d.Id, ct);
        if (atual is null)
        {
            if (await db.DiariosObra.AnyAsync(x => x.ObraId == d.ObraId && x.Data == d.Data, ct)) throw new RegraDeNegocioException($"Já existe diário em {d.Data:dd/MM/yyyy}.");
            atual = new DiarioObra { Id = d.Id, ObraId = d.ObraId, Data = d.Data };
            atual.Numero = (await db.DiariosObra.Where(x => x.ObraId == d.ObraId).MaxAsync(x => (int?)x.Numero, ct) ?? 0) + 1;
            db.DiariosObra.Add(atual);
        }
        else if (!atual.Editavel) throw new RegraDeNegocioException("Diário assinado não pode ser alterado.");
        atual.ClimaManha = d.ClimaManha; atual.ClimaTarde = d.ClimaTarde; atual.Condicao = d.Condicao; atual.Trabalhado = d.Trabalhado;
        atual.HoraInicio = d.HoraInicio; atual.HoraTermino = d.HoraTermino; atual.ObservacaoClima = d.ObservacaoClima; atual.Ocorrencias = d.Ocorrencias;
        atual.Visitas = d.Visitas; atual.Equipamentos = d.Equipamentos;
        // Filhos: substitui o efetivo; atividades mantêm o Id (as fotos são anexos da atividade).
        foreach (var e in atual.Efetivo.ToList()) { atual.Efetivo.Remove(e); db.Remove(e); }
        foreach (var e in d.Efetivo)
        {
            // Id já vem preenchido (Guid v7): adicionar pelo contexto, senão o EF trataria como alteração de linha existente.
            var n = new DiarioEfetivo { DiarioId = atual.Id, Terceiro = e.Terceiro, FuncaoObraId = e.FuncaoObraId, FornecedorId = e.FornecedorId, Nome = e.Nome, Quantidade = e.Quantidade };
            db.Add(n); atual.Efetivo.Add(n);
        }
        // Equipamentos do dia: substitui (horas somam no horímetro na assinatura).
        foreach (var e in atual.EquipamentosDia.ToList()) { atual.EquipamentosDia.Remove(e); db.Remove(e); }
        foreach (var e in d.EquipamentosDia.Where(e => e.EquipamentoId != Guid.Empty).GroupBy(e => e.EquipamentoId).Select(g => g.First()))
        {
            if (e.Horas < 0 || e.Horas > 24) throw new RegraDeNegocioException("Horas do equipamento devem estar entre 0 e 24.");
            var n = new DiarioEquipamento { DiarioId = atual.Id, EquipamentoId = e.EquipamentoId, Horas = e.Horas, Parado = e.Parado, Observacao = e.Observacao };
            db.Add(n); atual.EquipamentosDia.Add(n);
        }
        var manter = d.Atividades.Select(a => a.Id).ToHashSet();
        foreach (var a in atual.Atividades.Where(a => !manter.Contains(a.Id)).ToList()) { atual.Atividades.Remove(a); db.Remove(a); }
        foreach (var a in d.Atividades)
        {
            var x = atual.Atividades.FirstOrDefault(y => y.Id == a.Id);
            if (x is null) { x = new DiarioAtividade { Id = a.Id, DiarioId = atual.Id }; db.DiariosObraAtividades.Add(x); atual.Atividades.Add(x); }
            x.Ordem = a.Ordem; x.Descricao = a.Descricao; x.LocalId = a.LocalId; x.AtividadeChave = a.AtividadeChave;
        }
        await db.SaveChangesAsync(ct);
        return atual.Id;
    }

    /// <summary>Quem assina: o engenheiro da obra ou de uma SPE dela; sem engenheiro cadastrado, a Engenharia.</summary>
    public async Task<bool> PodeAssinarAsync(Guid obraId, CancellationToken ct = default)
    {
        await using var db = fabrica.CriarLeitura();
        return await PodeAssinarAsync(db, obraId, ct);
    }

    private async Task<bool> PodeAssinarAsync(ISigeoDb db, Guid obraId, CancellationToken ct)
    {
        if (usuario.UsuarioId is not { } id) return false;
        var engObra = await db.Obras.AsNoTracking().Where(o => o.Id == obraId).Select(o => o.EngenheiroId).FirstOrDefaultAsync(ct);
        var engSpes = await db.Spes.AsNoTracking().Where(s => s.ObraId == obraId && s.EngenheiroId != null).Select(s => s.EngenheiroId!.Value).ToListAsync(ct);
        if (engObra == id || engSpes.Contains(id)) return true;
        if (engObra is null && engSpes.Count == 0)
            return await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == id && (u.Perfil == PerfilUsuario.Engenharia || u.Perfil == PerfilUsuario.Administrador), ct);
        return false;
    }

    public async Task AssinarAsync(Guid diarioId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var d = await db.DiariosObra.Include(x => x.Efetivo).Include(x => x.Atividades).Include(x => x.EquipamentosDia).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == diarioId, ct)
                ?? throw new RegraDeNegocioException("Salve o diário antes de assinar.");
        await Permissoes.ExigirTelaAsync(db, usuario, "engenharia/diario", Contratos.AcaoTela.Aprovar, ct);
        if (!await PodeAssinarAsync(db, d.ObraId, ct)) throw new RegraDeNegocioException("Quem assina o diário é o engenheiro(a) da obra.");
        ValidacaoException.Se(d.Validar(Hoje));
        if (d.Trabalhado == DiaTrabalhado.Sim && d.Atividades.Count == 0) throw new RegraDeNegocioException("Dia trabalhado: registre ao menos uma atividade executada.");
        var nome = await db.Usuarios.AsNoTracking().Where(u => u.Id == usuario.UsuarioId).Select(u => u.Nome).FirstOrDefaultAsync(ct) ?? usuario.Nome;
        d.Assinar(usuario.UsuarioId!.Value, nome, relogio.GetUtcNow());
        await EquipamentosServico.HorasDoDiarioAsync(db, d, ct);
        await db.SaveChangesAsync(ct);
    }
}
