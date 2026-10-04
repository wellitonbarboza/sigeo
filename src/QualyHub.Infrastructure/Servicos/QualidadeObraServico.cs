using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Estrutura;
using QualyHub.Domain.Qualidade;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Contexto;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Casos de uso da qualidade da obra (Q01–Q15): modelos, FVS, RNC, ação corretiva, equipamentos, auditoria, ensaios e rastreabilidade.</summary>
public sealed class QualidadeObraServico(FabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private DateTimeOffset Agora => relogio.GetUtcNow();

    private static void Checar(IReadOnlyList<string> erros) => ValidacaoException.Se(erros);

    private async Task<PerfilUsuario> PerfilAsync(QualyHubDbContext db) =>
        usuario.UsuarioId is { } id ? await db.Usuarios.Where(u => u.Id == id).Select(u => u.Perfil).FirstOrDefaultAsync() : PerfilUsuario.Administrador;

    // ───────── Locais (Q03) ─────────

    public async Task<Guid> SalvarLocalAsync(Local l, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (l.ObraId == Guid.Empty) e.Add("Informe a obra.");
        if (string.IsNullOrWhiteSpace(l.Nome)) e.Add("Informe a descrição do local.");
        if (string.IsNullOrWhiteSpace(l.Codigo)) e.Add("Informe o código do local.");
        await using var db = fabrica.Criar();
        if (await db.Locais.AnyAsync(x => x.ObraId == l.ObraId && x.Codigo == l.Codigo && x.Id != l.Id, ct)) e.Add($"Código {l.Codigo} já existe nesta obra.");
        if (l.LocalPaiId == l.Id) e.Add("Local não pode ser pai de si mesmo.");
        // Condomínio, torre, pavimento, apartamento, cômodo e áreas comuns das tipologias nascem só pelo gerador.
        var gerados = Domain.Engenharia.GeradorEmpreendimento.Gerados;
        var existente = await db.Locais.AsNoTracking().FirstOrDefaultAsync(x => x.Id == l.Id, ct);
        if (existente is null && gerados.Contains(l.Tipo)) e.Add("Condomínio, torre, pavimento, apartamento e cômodo são criados pelo gerador do empreendimento (tipologias).");
        if (existente is not null && (gerados.Contains(existente.Tipo) || existente.AmbienteModeloId is not null) && (existente.Codigo != l.Codigo || existente.Tipo != l.Tipo || existente.LocalPaiId != l.LocalPaiId))
            e.Add("Local gerado pela estrutura do condomínio: só a descrição pode ser alterada.");
        Checar(e);
        l.LocalPai = null; l.Filhos = [];
        await Grafo.SalvarAsync(db, l);
        await db.SaveChangesAsync(ct);
        return l.Id;
    }

    // ───────── Modelos (Q01/Q02/Q12) ─────────

    public async Task<Guid> SalvarModeloAsync(ModeloInspecao m, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (string.IsNullOrWhiteSpace(m.Codigo)) throw new RegraDeNegocioException("Informe o código do modelo.");
        if (await db.ModelosInspecao.AnyAsync(x => x.Codigo == m.Codigo && x.Id != m.Id, ct)) throw new RegraDeNegocioException($"Já existe modelo {m.Codigo}.");
        var ordem = 1;
        foreach (var i in m.Itens.OrderBy(i => i.Ordem)) { i.Ordem = ordem++; i.ModeloInspecaoId = m.Id; }
        foreach (var x in m.Ensaios) x.ModeloInspecaoId = m.Id;
        await Grafo.SalvarAsync(db, m, nameof(ModeloInspecao.Itens), nameof(ModeloInspecao.Ensaios));
        await db.SaveChangesAsync(ct);
        return m.Id;
    }

    /// <summary>Publica (torna vigente); para FVM garante um único modelo vigente por material.</summary>
    public async Task PublicarModeloAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await db.ModelosInspecao.Include(x => x.Itens).Include(x => x.Ensaios).FirstAsync(x => x.Id == id, ct);
        Guid? revisaoDoc = null;
        if (m.DocumentoId is { } did) revisaoDoc = await db.Documentos.Where(d => d.Id == did).Select(d => d.RevisaoVigenteId).FirstOrDefaultAsync(ct);
        m.Publicar(revisaoDoc);
        if (m.Tipo == TipoModelo.FVM && m.MaterialControladoId is { } mat)
            foreach (var outro in await db.ModelosInspecao.Where(x => x.Id != m.Id && x.MaterialControladoId == mat && x.Situacao == SituacaoModelo.Vigente).ToListAsync(ct))
                outro.Situacao = SituacaoModelo.Obsoleto;
        if (m.Tipo == TipoModelo.FVS && m.ServicoControladoId is { } srv)
            foreach (var outro in await db.ModelosInspecao.Where(x => x.Id != m.Id && x.ServicoControladoId == srv && x.Tipo == TipoModelo.FVS && x.Situacao == SituacaoModelo.Vigente).ToListAsync(ct))
                outro.Situacao = SituacaoModelo.Obsoleto;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Nova revisão de ITS/especificação publicada → modelos ligados ao documento entram em revisão.</summary>
    public static async Task<int> DocumentoRevisadoAsync(QualyHubDbContext db, Guid documentoId, CancellationToken ct = default)
    {
        var ms = await db.ModelosInspecao.Where(m => m.DocumentoId == documentoId && m.Situacao == SituacaoModelo.Vigente).ToListAsync(ct);
        foreach (var m in ms) m.DocumentoRevisado();
        return ms.Count;
    }

    public async Task<ModeloInspecao?> ModeloFvmVigenteAsync(Guid materialControladoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        return await db.ModelosInspecao.AsNoTracking().Include(m => m.Itens).Include(m => m.Ensaios)
            .FirstOrDefaultAsync(m => m.Tipo == TipoModelo.FVM && m.MaterialControladoId == materialControladoId && m.Situacao == SituacaoModelo.Vigente, ct);
    }

    // ───────── FVS (Q04/Q05) ─────────

    public async Task<Guid> AbrirFvsAsync(Guid modeloId, Guid localId, Guid fornecedorId, Guid? contratoId, OrigemRegistro origem, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await db.ModelosInspecao.Include(x => x.Itens).FirstAsync(x => x.Id == modeloId, ct);
        var local = await db.Locais.FirstAsync(x => x.Id == localId, ct);
        if (fornecedorId == Guid.Empty) throw new RegraDeNegocioException("Informe o prestador.");
        if (m.ServicoControladoId is { } srv)
        {
            if (await db.Fvss.AnyAsync(f => f.LocalId == localId && f.ServicoControladoId == srv && f.Status != StatusFvs.Cancelada && f.Status != StatusFvs.Liberada, ct))
                throw new RegraDeNegocioException("Já existe FVS em andamento deste serviço neste local.");
            var anteriores = await db.Fvss.AsNoTracking().Include(f => f.Itens).Include(f => f.Modelo).Where(f => f.LocalId == localId && f.Status == StatusFvs.Reprovada).ToListAsync(ct);
            if (RegrasQualidadeObra.EtapaBloqueada(localId, srv, anteriores.Select(f => (f, f.Modelo!.ServicoSeguinteId))) is { } bloq)
                throw new RegraDeNegocioException(bloq);
        }
        var uid = usuario.UsuarioId ?? Guid.Empty;
        var f = Fvs.Abrir(m, local, fornecedorId, uid, usuario.Nome, Agora, $"FVS-{await Numerador.ProximoAsync(db, "Fvs", ct):0000}");
        f.ContratoId = contratoId; f.Origem = origem;
        db.Fvss.Add(f);
        await db.SaveChangesAsync(ct);
        return f.Id;
    }

    public static async Task<Fvs> CarregarFvsAsync(QualyHubDbContext db, Guid id, CancellationToken ct = default) =>
        await db.Fvss.Include(f => f.Itens).Include(f => f.Modelo).Include(f => f.Local).FirstAsync(f => f.Id == id, ct);

    /// <summary>Grava respostas parciais (rascunho da inspeção).</summary>
    public async Task SalvarFvsAsync(Fvs dados, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var f = await CarregarFvsAsync(db, dados.Id, ct);
        if (f.Status is not (StatusFvs.EmInspecao or StatusFvs.Reprovada)) throw new RegraDeNegocioException("FVS concluída não pode ser alterada.");
        if (f.Status == StatusFvs.EmInspecao)
        {
            foreach (var i in f.Itens)
            {
                var n = dados.Itens.First(x => x.Id == i.Id);
                i.Resultado = n.Resultado; i.ValorMedido = n.ValorMedido; i.Observacao = n.Observacao;
                i.AplicarValor();
            }
            f.EquipamentoId = dados.EquipamentoId;
        }
        f.Disposicao = dados.Disposicao; f.PrazoCorrecao = dados.PrazoCorrecao; f.ContratoId = dados.ContratoId;
        await db.SaveChangesAsync(ct);
    }

    public async Task ConcluirFvsAsync(Fvs dados, CancellationToken ct = default)
    {
        await SalvarFvsAsync(dados, ct);
        await using var db = fabrica.Criar();
        var f = await CarregarFvsAsync(db, dados.Id, ct);
        var equipOk = f.EquipamentoId is not { } eid || (await db.Equipamentos.AsNoTracking().FirstAsync(x => x.Id == eid, ct)).UsavelEm(Hoje);
        var itemIds = f.Itens.Select(i => i.Id).ToList();
        var fotos = await db.Anexos.AsNoTracking().Where(a => a.Entidade == "FvsItem" && itemIds.Contains(a.EntidadeId))
            .GroupBy(a => a.EntidadeId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        Checar(f.Concluir(equipOk, fotos));
        if (f.Status == StatusFvs.Reprovada) await AbrirRncDaFvsAsync(db, f, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task AbrirRncDaFvsAsync(QualyHubDbContext db, Fvs f, CancellationToken ct)
    {
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var nc = f.Itens.Where(i => i.Resultado == ResultadoItem.NaoConforme).ToList();
        var rnc = new Rnc
        {
            Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.Fvs, Referencia = $"{f.Numero} · itens {string.Join(", ", nc.Select(i => i.Ordem))}",
            ReferenciaId = f.Id, ObraId = f.ObraId, LocalId = f.LocalId, FornecedorId = f.FornecedorId, Classificacao = ClassificacaoRnc.Servico,
            Descricao = string.Join(" | ", nc.Select(i => $"{i.Descricao}: {i.Observacao}")), Disposicao = f.Disposicao,
            Gravidade = nc.Any(i => i.Bloqueia) ? Gravidade.Media : Gravidade.Baixa, ResponsavelId = usuario.UsuarioId, ResponsavelNome = usuario.Nome,
            Prazo = f.PrazoCorrecao ?? Hoje.AddDays(p.Int(Parametros.PrazoTratarRncDias)), Status = StatusRnc.EmTratamento
        };
        var desde = Agora.AddDays(-90);
        var reincidencias = await db.Rncs.CountAsync(r => r.FornecedorId == f.FornecedorId && r.Origem == OrigemRnc.Fvs && r.CriadoEm >= desde, ct);
        rnc.AvaliarExigencia(reincidencias + 1, p.Int(Parametros.ReincidenciasAc));
        db.Rncs.Add(rnc);
        f.RncId = rnc.Id;
    }

    public async Task ReinspecionarAsync(Guid fvsId, DateOnly data, IReadOnlyDictionary<Guid, ResultadoItem> resultados, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var f = await CarregarFvsAsync(db, fvsId, ct);
        Checar(f.Reinspecionar(data, resultados));
        if (f.Status == StatusFvs.Aprovada && f.RncId is { } rid)
        {
            var r = await db.Rncs.FirstAsync(x => x.Id == rid, ct);
            if (!r.ExigeAcao) { r.Status = StatusRnc.Encerrada; r.EncerradaPor = usuario.Nome; r.EncerradaEm = Agora; }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task LiberarFvsAsync(Guid fvsId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (await PerfilAsync(db) is not (PerfilUsuario.Engenharia or PerfilUsuario.Administrador or PerfilUsuario.Diretoria))
            throw new RegraDeNegocioException("A liberação da FVS é feita pelo engenheiro da obra.");
        var f = await db.Fvss.FirstAsync(x => x.Id == fvsId, ct);
        f.Liberar(usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora);
        await db.SaveChangesAsync(ct);
    }

    // ───────── RNC e ação corretiva (Q06/Q07) ─────────

    public async Task<Guid> SalvarRncAsync(Rnc r, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        if (r.Prazo == default) r.Prazo = Hoje.AddDays(p.Int(Parametros.PrazoTratarRncDias));
        if (r.Gravidade >= Gravidade.Alta) r.ExigeAcao = true;
        Checar(r.Validar());
        if (string.IsNullOrEmpty(r.Numero)) r.Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}";
        if (r.Status == StatusRnc.Aberta && r.CriadoEm != default) r.Status = StatusRnc.EmTratamento;
        await Grafo.SalvarAsync(db, r);
        await db.SaveChangesAsync(ct);
        return r.Id;
    }

    public async Task EncerrarRncAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var r = await db.Rncs.FirstAsync(x => x.Id == id, ct);
        var acOk = r.AcaoCorretivaId is { } aid && await db.AcoesCorretivas.AnyAsync(a => a.Id == aid && a.Status == StatusAcaoCorretiva.Encerrada, ct);
        r.Encerrar(usuario.Nome, Agora, acOk);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Abre ação corretiva a partir de uma ou mais RNC (ou de uma ocorrência de SST).</summary>
    public async Task<Guid> AbrirAcaoCorretivaAsync(IReadOnlyCollection<Guid> rncIds, Guid? ocorrenciaId, string problema, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var rncs = await db.Rncs.Where(r => rncIds.Contains(r.Id)).ToListAsync(ct);
        var ac = new AcaoCorretiva
        {
            Numero = $"AC-{await Numerador.ProximoAsync(db, "AcaoCorretiva", ct):0000}", Problema = problema,
            RncsOrigem = string.Join(", ", rncs.Select(r => r.Numero)), OcorrenciaId = ocorrenciaId,
            ResponsavelId = usuario.UsuarioId, ResponsavelNome = usuario.Nome, Prazo = Hoje.AddDays(p.Int(Parametros.PrazoTratarRncDias)),
            DataEficacia = Hoje.AddDays(p.Int(Parametros.PrazoTratarRncDias) + p.Int(Parametros.PrazoEficaciaAcDias))
        };
        db.AcoesCorretivas.Add(ac);
        foreach (var r in rncs) { r.AcaoCorretivaId = ac.Id; r.ExigeAcao = true; if (r.Status == StatusRnc.Aberta) r.Status = StatusRnc.EmTratamento; }
        if (ocorrenciaId is { } oid)
        {
            var o = await db.Ocorrencias.FirstAsync(x => x.Id == oid, ct);
            o.AcaoCorretivaId = ac.Id; o.Status = Domain.Common.StatusOcorrencia.EmInvestigacao;
        }
        await db.SaveChangesAsync(ct);
        return ac.Id;
    }

    public async Task SalvarAcaoCorretivaAsync(AcaoCorretiva a, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        foreach (var i in a.Itens) i.AcaoCorretivaId = a.Id;
        a.Atualizar();
        await Grafo.SalvarAsync(db, a, nameof(AcaoCorretiva.Itens));
        if (a.Status == StatusAcaoCorretiva.Encerrada)
        {
            foreach (var r in await db.Rncs.Where(r => r.AcaoCorretivaId == a.Id && r.Status != StatusRnc.Encerrada && r.Status != StatusRnc.Cancelada).ToListAsync(ct))
            { r.Status = StatusRnc.Encerrada; r.EncerradaPor = usuario.Nome; r.EncerradaEm = Agora; }
            foreach (var o in await db.Ocorrencias.Where(o => o.AcaoCorretivaId == a.Id && o.Status != Domain.Common.StatusOcorrencia.Encerrada).ToListAsync(ct))
                o.Status = Domain.Common.StatusOcorrencia.Encerrada;
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── Equipamentos (Q08/Q09) ─────────

    public async Task<Guid> SalvarEquipamentoAsync(EquipamentoMedicao e, CancellationToken ct = default)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(e.Codigo)) erros.Add("Informe o código (etiqueta).");
        if (string.IsNullOrWhiteSpace(e.Descricao)) erros.Add("Informe a descrição / modelo / nº de série.");
        if (string.IsNullOrWhiteSpace(e.Criterio)) erros.Add("Informe o critério de aceitação.");
        if (e.PeriodicidadeMeses <= 0) erros.Add("Informe a periodicidade.");
        if (e.TipoControle == TipoControleEquipamento.VerificacaoInterna && e.PadraoId is null) erros.Add("Verificação interna: informe o padrão usado.");
        Checar(erros);
        await using var db = fabrica.Criar();
        if (await db.Equipamentos.AnyAsync(x => x.Codigo == e.Codigo && x.Id != e.Id, ct)) throw new RegraDeNegocioException($"Código {e.Codigo} já existe.");
        e.Calibracoes = [];
        await Grafo.SalvarAsync(db, e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    /// <summary>Registra calibração/verificação. Reprovada: devolve as FVS feitas com o equipamento desde a última aprovação.</summary>
    public async Task<IReadOnlyList<string>> RegistrarCalibracaoAsync(Guid equipamentoId, Calibracao c, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var e = await db.Equipamentos.Include(x => x.Calibracoes).FirstAsync(x => x.Id == equipamentoId, ct);
        if (c.Tipo == TipoControleEquipamento.VerificacaoInterna && e.PadraoId is { } pid)
        {
            var padrao = await db.Equipamentos.AsNoTracking().FirstAsync(x => x.Id == pid, ct);
            if (!padrao.UsavelEm(c.Data)) throw new RegraDeNegocioException($"Padrão {padrao.Codigo} está com calibração vencida ou reprovada.");
        }
        var ultimaAprovada = e.Calibracoes.Where(x => x.Resultado == ResultadoCalibracao.Aprovado).Select(x => (DateOnly?)x.Data).Max();
        c.EquipamentoMedicaoId = e.Id; c.RegistradoPor = usuario.Nome;
        e.Registrar(c);
        db.Calibracoes.Add(c);
        IReadOnlyList<string> afetadas = [];
        if (c.Resultado == ResultadoCalibracao.Reprovado)
        {
            var desde = ultimaAprovada is { } d ? new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.MinValue;
            afetadas = await db.Fvss.AsNoTracking().Where(f => f.EquipamentoId == e.Id && f.DataInspecao >= desde).OrderBy(f => f.Numero).Select(f => f.Numero).ToListAsync(ct);
        }
        await db.SaveChangesAsync(ct);
        return afetadas;
    }

    // ───────── Auditoria interna (Q10/Q11) ─────────

    public async Task<Guid> SalvarAuditoriaAsync(AuditoriaInterna a, CancellationToken ct = default)
    {
        Checar(a.Validar());
        await using var db = fabrica.Criar();
        if (string.IsNullOrEmpty(a.Codigo)) a.Codigo = $"AUD-{a.Inicio.Year}-{await Numerador.ProximoAsync(db, $"Auditoria-{a.Inicio.Year}", ct):00}";
        if (a.Status == StatusAuditoria.Planejada && a.Constatacoes.Any(c => c.Tipo != TipoConstatacao.NaoAvaliado)) a.Status = StatusAuditoria.EmExecucao;
        var ordem = 1;
        foreach (var c in a.Constatacoes) { c.AuditoriaInternaId = a.Id; c.Ordem = ordem++; }
        await Grafo.SalvarAsync(db, a, nameof(AuditoriaInterna.Constatacoes));
        await db.SaveChangesAsync(ct);
        return a.Id;
    }

    /// <summary>Emite relatório: cada constatação "Não conformidade" abre RNC com origem auditoria interna.</summary>
    public async Task<int> EmitirRelatorioAuditoriaAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var a = await db.AuditoriasInternas.Include(x => x.Constatacoes).FirstAsync(x => x.Id == id, ct);
        a.EmitirRelatorio();
        var n = 0;
        foreach (var c in a.Constatacoes.Where(c => c.Tipo == TipoConstatacao.NaoConformidade && c.RncId is null))
        {
            var r = new Rnc
            {
                Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.AuditoriaInterna, Referencia = $"{a.Codigo} · {c.Requisito}",
                ReferenciaId = a.Id, Descricao = $"{c.Pergunta} — {c.Evidencia}", Classificacao = ClassificacaoRnc.Sistema, Gravidade = Gravidade.Media,
                Disposicao = "Tratar conforme ação corretiva", ExigeAcao = true, ResponsavelNome = "Qualidade", Prazo = Hoje.AddDays(p.Int(Parametros.PrazoTratarRncDias) * 2)
            };
            db.Rncs.Add(r); c.RncId = r.Id; n++;
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    public async Task EncerrarAuditoriaAsync(Guid id, string resumo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.AuditoriasInternas.FirstAsync(x => x.Id == id, ct);
        if (a.Status != StatusAuditoria.RelatorioEmitido) throw new RegraDeNegocioException("Emita o relatório antes de encerrar.");
        a.ResumoAnaliseCritica = resumo; a.Status = StatusAuditoria.Encerrada;
        await db.SaveChangesAsync(ct);
    }

    // ───────── Ensaios e rastreabilidade (Q14/Q15) ─────────

    public async Task RegistrarEnsaioAsync(Guid ensaioId, string resultado, bool aprovado, Guid? laboratorioId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resultado)) throw new RegraDeNegocioException("Informe o resultado do ensaio.");
        await using var db = fabrica.Criar();
        var x = await db.EnsaiosRecebimento.FirstAsync(e => e.Id == ensaioId, ct);
        if (x.Situacao != SituacaoEnsaio.Aguardando) throw new RegraDeNegocioException("Ensaio já registrado.");
        var rec = await db.Recebimentos.Include(r => r.Ensaios).Include(r => r.Aplicacoes).FirstAsync(r => r.Id == x.RecebimentoId, ct);
        x.Resultado = resultado; x.LaboratorioId = laboratorioId; x.Situacao = aprovado ? SituacaoEnsaio.Aprovado : SituacaoEnsaio.Reprovado;
        if (!aprovado)
        {
            var locais = await db.Locais.AsNoTracking().Where(l => rec.Aplicacoes.Select(a => a.LocalId).Contains(l.Id)).Select(l => l.Nome).ToListAsync(ct);
            var r = new Rnc
            {
                Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.FvmRecebimento, Referencia = $"Recebimento {rec.Numero} · {x.Ensaio} · {x.Amostra}",
                ReferenciaId = rec.Id, ObraId = rec.ObraId, FornecedorId = rec.FornecedorId, Classificacao = ClassificacaoRnc.Material, Gravidade = Gravidade.Alta, ExigeAcao = true,
                Descricao = $"Ensaio {x.Ensaio} reprovado: {resultado} (critério: {x.Criterio}).", LocaisAfetados = locais.Count == 0 ? null : string.Join("; ", locais),
                Disposicao = x.AcaoReprovacao == AcaoReprovacaoEnsaio.RecusarCarga ? "Devolução ao fornecedor" : "Avaliar locais afetados (projetista)",
                ResponsavelNome = "Engenharia", Prazo = Hoje.AddDays(3)
            };
            db.Rncs.Add(r); x.RncId = r.Id;
        }
        rec.StatusLote = RegrasQualidadeObra.StatusDoLote(rec.FvmAprovada == true, rec.Destino == DestinoRecebimento.Devolvido, false, rec.QuarentenaAteEnsaio, [.. rec.Ensaios]);
        await db.SaveChangesAsync(ct);
    }

    public async Task AplicarLoteAsync(Guid recebimentoId, Guid localId, decimal quantidade, DateOnly data, Guid? fvsId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var rec = await db.Recebimentos.Include(r => r.Itens).Include(r => r.Ensaios).Include(r => r.Aplicacoes).FirstAsync(r => r.Id == recebimentoId, ct);
        Checar(RegrasQualidadeObra.PodeAplicar(rec.StatusLote, rec.QuarentenaAteEnsaio, rec.Ensaios, rec.QuantidadeRecebida, rec.Aplicacoes.Sum(a => a.Quantidade), quantidade));
        db.AplicacoesLote.Add(new AplicacaoLote { RecebimentoId = rec.Id, LocalId = localId, Quantidade = quantidade, Data = data, FvsId = fvsId, RegistradoPor = usuario.Nome, RegistradoEm = Agora });
        await db.SaveChangesAsync(ct);
    }
}
