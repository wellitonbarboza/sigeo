using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;
using Sigeo.Domain.Planejamento;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Equipamentos (patrimônio próprio e locados), movimentação, planos preventivos e ordens de serviço.</summary>
public sealed class EquipamentosServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private DateTimeOffset Agora => relogio.GetUtcNow();

    // ───────── Famílias (configuráveis) ─────────

    public async Task SalvarFamiliaAsync(FamiliaEquipamento f, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(f.Nome)) throw new RegraDeNegocioException("Informe o nome da família.");
        await using var db = fabrica.Criar();
        if (await db.FamiliasEquipamento.AnyAsync(x => x.Nome == f.Nome && x.Id != f.Id, ct)) throw new RegraDeNegocioException($"Família {f.Nome} já existe.");
        await Grafo.SalvarAsync(db, f);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Equipamento ─────────

    public async Task<Guid> SalvarAsync(Equipamento e, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        foreach (var d in e.Documentos) d.EquipamentoId = e.Id;
        var erros = e.Validar().ToList();
        if (await db.Ativos.AnyAsync(x => x.Patrimonio == e.Patrimonio && x.Id != e.Id, ct)) erros.Add($"Patrimônio {e.Patrimonio} já cadastrado.");
        var fam = await db.FamiliasEquipamento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == e.FamiliaId, ct);
        if (fam is { Ativa: false } && !await db.Ativos.AnyAsync(x => x.Id == e.Id, ct)) erros.Add($"A família {fam.Nome} está desligada: ligue-a em Famílias para cadastrar.");
        if (e.Quantidade <= 0) erros.Add("Quantidade deve ser maior que zero.");
        ValidacaoException.Se(erros);
        var novo = !await db.Ativos.AnyAsync(x => x.Id == e.Id, ct);
        await Grafo.SalvarAsync(db, e, nameof(Equipamento.Documentos));
        if (novo && e.AlmoxarifadoId is not null)
            db.MovimentosEquipamento.Add(new MovimentoEquipamento { EquipamentoId = e.Id, Data = Hoje, Tipo = TipoMovimentoEquipamento.Chegada, ParaAlmoxarifadoId = e.AlmoxarifadoId, Observacao = "Cadastro" });
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    /// <summary>
    /// Movimenta o equipamento: chegada, transferência entre almoxarifados (obras/central), devolução à locadora, envio/retorno de manutenção.
    /// Antes de mobilizar para outra obra, plano por evento vira OS obrigatória.
    /// </summary>
    public async Task MovimentarAsync(Guid equipamentoId, TipoMovimentoEquipamento tipo, Guid? paraAlmoxarifadoId, DateOnly data, string? checklist, string? observacao, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var e = await db.Ativos.FirstAsync(x => x.Id == equipamentoId, ct);
        if (e.Situacao is SituacaoEquipamento.Baixado or SituacaoEquipamento.Devolvido && tipo != TipoMovimentoEquipamento.Chegada)
            throw new RegraDeNegocioException("Equipamento baixado ou devolvido.");
        if (tipo is TipoMovimentoEquipamento.Transferencia or TipoMovimentoEquipamento.Chegada && paraAlmoxarifadoId is null) throw new RegraDeNegocioException("Informe para onde vai.");
        if (tipo == TipoMovimentoEquipamento.Transferencia)
        {
            var eventos = await db.PlanosManutencao.Where(p => p.EquipamentoId == e.Id && p.Ativo && p.Gatilho == GatilhoManutencao.Evento).ToListAsync(ct);
            foreach (var p in eventos)
            {
                var feita = await db.OrdensServico.AnyAsync(o => o.PlanoId == p.Id && o.Status == StatusOrdemServico.Verificada && o.VerificadaEm >= Agora.AddDays(-15), ct);
                if (!feita) throw new RegraDeNegocioException($"Antes de mobilizar: faça e verifique a OS do plano \"{p.Nome}\" (gere em Preventivas).");
            }
        }
        db.MovimentosEquipamento.Add(new MovimentoEquipamento
        {
            EquipamentoId = e.Id, Data = data, Tipo = tipo, DeAlmoxarifadoId = e.AlmoxarifadoId, ParaAlmoxarifadoId = paraAlmoxarifadoId,
            Checklist = checklist, Observacao = observacao
        });
        switch (tipo)
        {
            case TipoMovimentoEquipamento.Chegada: e.AlmoxarifadoId = paraAlmoxarifadoId; e.Situacao = SituacaoEquipamento.EmUso; break;
            case TipoMovimentoEquipamento.Transferencia: e.AlmoxarifadoId = paraAlmoxarifadoId; break;
            case TipoMovimentoEquipamento.Devolucao: e.Situacao = SituacaoEquipamento.Devolvido; break;
            case TipoMovimentoEquipamento.EnvioManutencao: e.Situacao = SituacaoEquipamento.EmManutencao; break;
            case TipoMovimentoEquipamento.RetornoManutencao: e.Situacao = SituacaoEquipamento.EmUso; if (paraAlmoxarifadoId is not null) e.AlmoxarifadoId = paraAlmoxarifadoId; break;
            case TipoMovimentoEquipamento.Baixa:
                if (string.IsNullOrWhiteSpace(observacao)) throw new RegraDeNegocioException("Baixa: informe o motivo.");
                e.Situacao = SituacaoEquipamento.Baixado; break;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarLeituraAsync(Guid equipamentoId, decimal? horimetro, decimal? hodometro, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var e = await db.Ativos.FirstAsync(x => x.Id == equipamentoId, ct);
        if (horimetro is { } h) { if (h < e.Horimetro) throw new RegraDeNegocioException("Horímetro menor que o atual."); e.Horimetro = h; }
        if (hodometro is { } k) { if (k < e.Hodometro) throw new RegraDeNegocioException("Hodômetro menor que o atual."); e.Hodometro = k; }
        await db.SaveChangesAsync(ct);
    }

    // ───────── Planos preventivos ─────────

    public async Task SalvarPlanoAsync(PlanoManutencao p, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(p.Nome)) e.Add("Informe o nome do plano.");
        if (p.Gatilho == GatilhoManutencao.Tempo && p.IntervaloDias is not > 0) e.Add("Por tempo: informe o intervalo em dias.");
        if (p.Gatilho == GatilhoManutencao.Uso && p.IntervaloUso is not > 0) e.Add("Por uso: informe o intervalo (horas ou km).");
        if (string.IsNullOrWhiteSpace(p.Executante)) e.Add("Informe quem executa.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (p.UltimaData is null && p.Gatilho == GatilhoManutencao.Tempo) p.UltimaData = Hoje;
        if (p.UltimaLeitura is null && p.Gatilho == GatilhoManutencao.Uso)
            p.UltimaLeitura = await db.Ativos.Where(x => x.Id == p.EquipamentoId).Select(x => x.Horimetro + x.Hodometro).FirstAsync(ct);
        await Grafo.SalvarAsync(db, p);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Gera a OS preventiva do plano (se ainda não houver uma aberta).</summary>
    public async Task<Guid> GerarOsAsync(Guid planoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await db.PlanosManutencao.AsNoTracking().FirstAsync(x => x.Id == planoId, ct);
        if (await db.OrdensServico.AnyAsync(o => o.PlanoId == planoId && (o.Status == StatusOrdemServico.Aberta || o.Status == StatusOrdemServico.EmExecucao || o.Status == StatusOrdemServico.AguardandoPeca), ct))
            throw new RegraDeNegocioException("Já há uma OS aberta para este plano.");
        var os = new OrdemServico
        {
            EquipamentoId = p.EquipamentoId, PlanoId = p.Id, ContratoId = p.ContratoId, Tipo = TipoOrdemServico.Preventiva, AbertaEm = Agora, AbertaPor = usuario.Nome,
            Descricao = p.Nome, Numero = await Numerador.ProximoAsync(db, "OrdemServico", ct)
        };
        db.OrdensServico.Add(os);
        await db.SaveChangesAsync(ct);
        return os.Id;
    }

    /// <summary>Gera as OS dos planos que vencem dentro da antecedência configurada (rotina diária ou botão).</summary>
    public async Task<int> GerarPreventivasAsync(CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var planos = await db.PlanosManutencao.AsNoTracking().Where(p => p.Ativo && p.Gatilho != GatilhoManutencao.Evento).ToListAsync(ct);
        var leituras = await db.Ativos.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Horimetro + a.Hodometro, ct);
        var abertas = await db.OrdensServico.AsNoTracking().Where(o => o.PlanoId != null && (o.Status == StatusOrdemServico.Aberta || o.Status == StatusOrdemServico.EmExecucao || o.Status == StatusOrdemServico.AguardandoPeca))
            .Select(o => o.PlanoId!.Value).ToListAsync(ct);
        var n = 0;
        foreach (var p in planos.Where(p => !abertas.Contains(p.Id)))
        {
            var leitura = leituras.GetValueOrDefault(p.EquipamentoId);
            if (!p.Vencido(Hoje, leitura) && !p.PertoDeVencer(Hoje, leitura)) continue;
            db.OrdensServico.Add(new OrdemServico
            {
                EquipamentoId = p.EquipamentoId, PlanoId = p.Id, ContratoId = p.ContratoId, Tipo = TipoOrdemServico.Preventiva, AbertaEm = Agora, AbertaPor = "Rotina",
                Descricao = p.Nome, Numero = await Numerador.ProximoAsync(db, "OrdemServico", ct)
            });
            n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    // ───────── Ordens de serviço ─────────

    /// <summary>Abre corretiva (campo: "equipamento parou"). Parado = conta o tempo parado e bloqueia o uso.</summary>
    public async Task<Guid> AbrirCorretivaAsync(Guid equipamentoId, string descricao, bool parado, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new RegraDeNegocioException("Descreva o que aconteceu.");
        await using var db = fabrica.Criar();
        var e = await db.Ativos.FirstAsync(x => x.Id == equipamentoId, ct);
        var os = new OrdemServico
        {
            EquipamentoId = e.Id, Tipo = TipoOrdemServico.Corretiva, AbertaEm = Agora, AbertaPor = usuario.Nome, Descricao = descricao.Trim(),
            ParadoDesde = parado ? Agora : null, Numero = await Numerador.ProximoAsync(db, "OrdemServico", ct)
        };
        if (parado) e.Situacao = SituacaoEquipamento.Parado;
        db.OrdensServico.Add(os);
        // Equipamento parado em obra: restrição de equipamento no Last Planner até a OS ser verificada.
        if (parado && e.AlmoxarifadoId is { } al && await db.Almoxarifados.AsNoTracking().Where(a => a.Id == al).Select(a => a.ObraId).FirstOrDefaultAsync(ct) is { } obra)
        {
            var r = new Restricao
            {
                ObraId = obra, Tipo = TipoRestricao.Equipamento, Descricao = $"{e.Patrimonio} {e.Nome} parado: {descricao.Trim()}", AtividadeChave = $"EQP:{e.Id:N}",
                AtividadeNome = $"Equipamento {e.Patrimonio}", ResponsavelId = usuario.UsuarioId, ResponsavelNome = string.IsNullOrWhiteSpace(usuario.Nome) ? "Manutenção" : usuario.Nome,
                DataLimite = Hoje.AddDays(e.Criticidade == Criticidade.A ? 1 : 3), OrigemId = os.Id, Numero = await Numerador.ProximoAsync(db, "Restricao", ct)
            };
            db.Restricoes.Add(r);
        }
        await db.SaveChangesAsync(ct);
        return os.Id;
    }

    public async Task SalvarOsAsync(OrdemServico os, CancellationToken ct = default)
    {
        if (os.CustoMaoObra < 0 || os.CustoPecas < 0 || os.CustoTerceiros < 0) throw new RegraDeNegocioException("Custos não podem ser negativos.");
        await using var db = fabrica.Criar();
        var atual = await db.OrdensServico.FirstAsync(x => x.Id == os.Id, ct);
        if (atual.Status is StatusOrdemServico.Verificada or StatusOrdemServico.Cancelada) throw new RegraDeNegocioException("OS encerrada.");
        atual.Descricao = os.Descricao; atual.ModoFalha = os.ModoFalha; atual.Causa = os.Causa; atual.CustoMaoObra = os.CustoMaoObra; atual.CustoPecas = os.CustoPecas;
        atual.CustoTerceiros = os.CustoTerceiros; atual.ChecklistResultado = os.ChecklistResultado; atual.Leitura = os.Leitura; atual.Observacao = os.Observacao;
        atual.ContratoId = os.ContratoId; atual.RequisicaoId = os.RequisicaoId; atual.ExecutadoPor = os.ExecutadoPor;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Avança a OS. Verificada: libera o equipamento, atualiza o plano (última data/leitura) e remove a restrição do Last Planner.</summary>
    public async Task AvancarOsAsync(Guid osId, StatusOrdemServico novo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var os = await db.OrdensServico.FirstAsync(x => x.Id == osId, ct);
        os.Avancar(novo, usuario.Nome, Agora);
        var e = await db.Ativos.FirstAsync(x => x.Id == os.EquipamentoId, ct);
        if (novo == StatusOrdemServico.EmExecucao && os.Tipo == TipoOrdemServico.Preventiva && os.ParadoDesde is null) os.ParadoDesde = Agora;
        if (novo == StatusOrdemServico.Verificada)
        {
            if (os.Leitura is { } l && l > e.Horimetro && e.Hodometro == 0) e.Horimetro = l;
            if (e.Situacao is SituacaoEquipamento.Parado or SituacaoEquipamento.EmManutencao) e.Situacao = e.AlmoxarifadoId is null ? SituacaoEquipamento.Disponivel : SituacaoEquipamento.EmUso;
            if (os.PlanoId is { } pid)
            {
                var p = await db.PlanosManutencao.FirstAsync(x => x.Id == pid, ct);
                p.UltimaData = Hoje; p.UltimaLeitura = os.Leitura ?? (e.Horimetro + e.Hodometro);
            }
            await MotorEstoque.RemoverRestricoesAsync(db, os.Id, $"OS {os.Numero} verificada: equipamento liberado.", usuario.Nome, Agora, ct);
            // Contrato de manutenção: a OS da visita fica ligada à parcela do período (permite aprovar).
            if (os.ContratoId is { } cid)
            {
                var dia = DateOnly.FromDateTime((os.ConcluidaEm ?? Agora).LocalDateTime);
                var parcela = await db.ParcelasContrato.FirstOrDefaultAsync(x => x.ContratoId == cid && x.Inicio <= dia && x.Fim >= dia && x.OrdemServicoId == null, ct);
                if (parcela is not null) parcela.OrdemServicoId = os.Id;
            }
        }
        if (novo == StatusOrdemServico.Cancelada && e.Situacao == SituacaoEquipamento.Parado) e.Situacao = SituacaoEquipamento.EmUso;
        await db.SaveChangesAsync(ct);
    }

    // ───────── Situação ─────────

    public sealed record Situacao(Equipamento Equipamento, IReadOnlyList<string> Bloqueios, IReadOnlyList<PlanoManutencao> Planos);

    public static async Task<List<Situacao>> SituacoesAsync(ISigeoDb db, DateOnly hoje, CancellationToken ct = default)
    {
        var eqs = await db.Ativos.AsNoTracking().Include(e => e.Documentos).ToListAsync(ct);
        var planos = await db.PlanosManutencao.AsNoTracking().Where(p => p.Ativo).ToListAsync(ct);
        var abertas = await db.OrdensServico.AsNoTracking().Where(o => o.Status == StatusOrdemServico.Aberta || o.Status == StatusOrdemServico.EmExecucao || o.Status == StatusOrdemServico.AguardandoPeca).ToListAsync(ct);
        return [.. eqs.Select(e => new Situacao(e, RegrasManutencao.Bloqueios(e, planos, abertas, hoje), planos.Where(p => p.EquipamentoId == e.Id).ToList()))];
    }

    /// <summary>Diário de obra assinado: horas trabalhadas somam no horímetro.</summary>
    public static async Task HorasDoDiarioAsync(ISigeoDb db, DiarioObra d, CancellationToken ct = default)
    {
        foreach (var x in d.EquipamentosDia.Where(x => x.Horas > 0))
        {
            var e = await db.Ativos.FirstOrDefaultAsync(a => a.Id == x.EquipamentoId, ct);
            if (e is not null && e.Hodometro == 0) e.Horimetro += x.Horas;
        }
    }
}
