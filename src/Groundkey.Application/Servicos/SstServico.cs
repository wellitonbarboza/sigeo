using Microsoft.EntityFrameworkCore;
using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;
using Groundkey.Domain.Estrutura;
using Groundkey.Domain.Qualidade;
using Groundkey.Domain.Sst;

namespace Groundkey.Application.Servicos;

/// <summary>Casos de uso de segurança e pessoas (S01–S13). CPF nunca em claro; ASO só conclusão e aptidões.</summary>
public sealed class SstServico(IFabricaDb fabrica, IUsuarioAtual usuario, TimeProvider relogio, ProtecaoCpf cpf, IArmazenamento armazenamento)
{
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);
    private DateTimeOffset Agora => relogio.GetUtcNow();

    // ───────── Colaboradores ─────────

    public async Task<Guid> SalvarColaboradorAsync(Colaborador c, string? cpfDigitado, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(c.Nome)) e.Add("Informe o nome.");
        if (c.FuncaoObraId == Guid.Empty) e.Add("Informe a função.");
        if (c.ObraId == Guid.Empty) e.Add("Informe a obra.");
        if (c.Vinculo == VinculoColaborador.Terceiro && c.FornecedorId is null) e.Add("Terceiro: informe a empresa.");
        if (!string.IsNullOrWhiteSpace(c.Telefone) && !c.ConsentimentoTelefone) e.Add("Telefone só com consentimento registrado (LGPD).");
        await using var db = fabrica.Criar();
        if (!string.IsNullOrWhiteSpace(cpfDigitado))
        {
            var d = Conversor.SomenteDigitos(cpfDigitado);
            if (!Conversor.CpfValido(d)) e.Add("CPF inválido.");
            else
            {
                c.CpfHash = cpf.Hash(d); c.CpfMascarado = ProtecaoCpf.Mascarar(d);
                if (await db.Colaboradores.IgnoreQueryFilters(["obra"]).AnyAsync(x => x.CpfHash == c.CpfHash && x.ObraId == c.ObraId && x.Id != c.Id && !x.Excluido, ct))
                    e.Add("Já existe cadastro com este CPF nesta obra.");
            }
        }
        else if (string.IsNullOrEmpty(c.CpfHash)) e.Add("Informe o CPF.");
        ValidacaoException.Se(e);
        await Grafo.SalvarAsync(db, c);
        await db.SaveChangesAsync(ct);
        await RecalcularAsync([c.Id], ct);
        return c.Id;
    }

    /// <summary>Recalcula situação e restrições pelas exigências da função (matriz S03A).</summary>
    public async Task<IReadOnlyDictionary<Guid, SituacaoCalculada>> RecalcularAsync(IReadOnlyCollection<Guid>? ids = null, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var q = db.Colaboradores.AsQueryable();
        if (ids is not null) q = q.Where(c => ids.Contains(c.Id));
        var cols = await q.ToListAsync(ct);
        var cid = cols.Select(c => c.Id).ToList();
        var matriz = await db.FuncaoExigencias.AsNoTracking().ToListAsync(ct);
        var exig = await db.Exigencias.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
        var docs = await db.DocumentosColaborador.AsNoTracking().Where(d => cid.Contains(d.ColaboradorId)).ToListAsync(ct);
        var parts = await db.Participantes.AsNoTracking().Where(p => cid.Contains(p.ColaboradorId) && p.Resultado == ResultadoParticipante.Aprovado)
            .Join(db.Turmas, p => p.TurmaId, t => t.Id, (p, t) => new { p.ColaboradorId, t.TreinamentoId, p.ValidoAte }).ToListAsync(ct);
        var epis = await db.EntregasEpi.AsNoTracking().Where(x => cid.Contains(x.ColaboradorId)).Select(x => x.ColaboradorId).Distinct().ToListAsync(ct);
        var r = new Dictionary<Guid, SituacaoCalculada>();
        foreach (var c in cols)
        {
            var s = RegrasSst.Calcular(c, matriz, exig, docs.Where(d => d.ColaboradorId == c.Id), parts.Where(p => p.ColaboradorId == c.Id).Select(p => (p.TreinamentoId, p.ValidoAte)), epis.Contains(c.Id), Hoje);
            c.Situacao = s.Situacao; c.Restricoes = s.Restricoes;
            c.Pendencias = string.Join("; ", s.Detalhe.Where(d => !d.Ok).Select(d => $"{d.Exigencia} ({d.Situacao.ToLowerInvariant()})"));
            r[c.Id] = s;
        }
        await db.SaveChangesAsync(ct);
        return r;
    }

    // ───────── Matriz de exigências por função (S03A) ─────────

    /// <summary>Validação pura do cadastro de exigência: sem fonte normativa a exigência não se sustenta em auditoria.</summary>
    public static IReadOnlyList<string> ValidarExigencia(Exigencia x)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(x.Descricao) || string.IsNullOrWhiteSpace(x.Fonte)) e.Add("Informe a exigência e a fonte (NR/PGR/PCMSO).");
        return e;
    }

    public static IReadOnlyList<string> ValidarFuncao(FuncaoObra f) =>
        string.IsNullOrWhiteSpace(f.Descricao) ? ["Informe a função."] : [];

    public async Task SalvarExigenciaAsync(Exigencia x, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarExigencia(x));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, x);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarFuncaoAsync(FuncaoObra f, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarFuncao(f));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, f, nameof(FuncaoObra.Exigencias));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Grava a matriz inteira de uma vez e recalcula todos: mudar a exigência de uma função muda o bloqueio de quem a exerce.</summary>
    public async Task SalvarMatrizAsync(IEnumerable<FuncaoObra> funcoes, CancellationToken ct = default)
    {
        await using (var db = fabrica.Criar())
        {
            foreach (var f in funcoes) await Grafo.SalvarAsync(db, f, nameof(FuncaoObra.Exigencias));
            await db.SaveChangesAsync(ct);
        }
        await RecalcularAsync(null, ct);
    }

    // ───────── Documentos (S04) ─────────

    public async Task<Guid> SalvarDocumentoAsync(DocumentoColaborador d, Stream? arquivo, string? nome, string? contentType, bool validarJa, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var ex = await db.Exigencias.AsNoTracking().FirstAsync(x => x.Id == d.ExigenciaId, ct);
        ValidacaoException.Se(d.Validar(ex));
        if (arquivo is not null && nome is not null)
        {
            var s = await armazenamento.SalvarAsync(arquivo, nome, ex.Tipo == TipoExigencia.ExameAso ? "sst-restrito" : "sst", contentType, ct);
            d.ArquivoBlob = s.Blob; d.ArquivoNome = nome;
        }
        if (d.ArquivoBlob is null) throw new RegraDeNegocioException("Anexe o arquivo do documento.");
        if (validarJa) d.Decidir(true, null, usuario.Nome, Agora);
        await Grafo.SalvarAsync(db, d);
        await db.SaveChangesAsync(ct);
        await RecalcularAsync([d.ColaboradorId], ct);
        return d.Id;
    }

    public async Task ValidarDocumentoAsync(Guid id, bool valido, string? motivo, ConclusaoAso? conclusao, AtividadeRisco apto, DateOnly? validade, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var d = await db.DocumentosColaborador.FirstAsync(x => x.Id == id, ct);
        var ex = await db.Exigencias.AsNoTracking().FirstAsync(x => x.Id == d.ExigenciaId, ct);
        if (ex.Tipo == TipoExigencia.ExameAso) { d.ConclusaoAso = conclusao; d.AptoAtividades = apto; }
        if (validade is not null) d.Validade = validade;
        if (valido) ValidacaoException.Se(d.Validar(ex));
        d.Decidir(valido, motivo, usuario.Nome, Agora);
        await db.SaveChangesAsync(ct);
        await RecalcularAsync([d.ColaboradorId], ct);
    }

    // ───────── Treinamentos (S05–S07) ─────────

    public async Task<Guid> SalvarTurmaAsync(Turma t, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (t.TreinamentoId == Guid.Empty) e.Add("Informe o treinamento.");
        if (string.IsNullOrWhiteSpace(t.Instrutor)) e.Add("Informe o instrutor.");
        if (string.IsNullOrWhiteSpace(t.Local)) e.Add("Informe o local.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (string.IsNullOrEmpty(t.Codigo)) t.Codigo = $"TR-{await Numerador.ProximoAsync(db, "Turma", ct):0000}";
        foreach (var p in t.Participantes) p.TurmaId = t.Id;
        await Grafo.SalvarAsync(db, t, nameof(Turma.Participantes));
        await db.SaveChangesAsync(ct);
        return t.Id;
    }

    public async Task ConcluirTurmaAsync(Turma dados, CancellationToken ct = default)
    {
        await SalvarTurmaAsync(dados, ct);
        await using var db = fabrica.Criar();
        var t = await db.Turmas.Include(x => x.Participantes).FirstAsync(x => x.Id == dados.Id, ct);
        var tr = await db.Treinamentos.AsNoTracking().FirstAsync(x => x.Id == t.TreinamentoId, ct);
        t.Concluir(tr);
        await db.SaveChangesAsync(ct);
        await RecalcularAsync(t.Participantes.Select(p => p.ColaboradorId).ToList(), ct);
    }

    public static IReadOnlyList<string> ValidarTreinamento(Treinamento t) =>
        string.IsNullOrWhiteSpace(t.Descricao) || string.IsNullOrWhiteSpace(t.CargaHoraria) ? ["Informe o treinamento e a carga horária."] : [];

    public async Task SalvarTreinamentoAsync(Treinamento t, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarTreinamento(t));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, t);
        await db.SaveChangesAsync(ct);
    }

    // ───────── EPI (S08) ─────────

    public static IReadOnlyList<string> ValidarEpi(Epi x) =>
        string.IsNullOrWhiteSpace(x.Descricao) || string.IsNullOrWhiteSpace(x.Ca) ? ["Informe a descrição e o CA."] : [];

    public async Task SalvarEpiAsync(Epi x, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarEpi(x));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, x);
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEntregaEpiAsync(Guid colaboradorId, Guid epiId, decimal qtd, DateOnly data, MotivoEntregaEpi motivo, string assinatura, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var epi = await db.Epis.AsNoTracking().FirstAsync(x => x.Id == epiId, ct);
        var entrega = EntregaEpi.Registrar(epi, colaboradorId, qtd, data, motivo, assinatura, usuario.Nome);
        db.EntregasEpi.Add(entrega);
        // EPI com produto controlado no estoque da obra: a entrega baixa do almoxarifado da obra do colaborador.
        if (epi.ProdutoId is { } prod)
        {
            var col = await db.Colaboradores.AsNoTracking().Where(c => c.Id == colaboradorId).Select(c => new { c.ObraId, c.Nome, c.FornecedorId }).FirstAsync(ct);
            var almox = await db.Almoxarifados.AsNoTracking().Where(a => a.ObraId == col.ObraId).Select(a => (Guid?)a.Id).FirstOrDefaultAsync(ct);
            if (almox is { } al && await db.SaldosEstoque.AnyAsync(s => s.AlmoxarifadoId == al && s.ProdutoId == prod, ct))
                await MotorEstoque.MovimentarAsync(db, new Domain.Suprimentos.MovimentoEstoque
                {
                    AlmoxarifadoId = al, ProdutoId = prod, Data = Agora, Tipo = Domain.Suprimentos.TipoMovimentoEstoque.EntregaEpi, Quantidade = -qtd,
                    ColaboradorId = colaboradorId, FornecedorId = col.FornecedorId, EntreguePara = col.Nome, Documento = $"Entrega de EPI · {epi.Descricao}",
                    OrigemTipo = "EntregaEpi", OrigemId = entrega.Id
                }, ct);
        }
        await db.SaveChangesAsync(ct);
        await RecalcularAsync([colaboradorId], ct);
    }

    // ───────── Inspeções de segurança (S09/S10) ─────────

    public async Task<Guid> AbrirInspecaoAsync(Guid modeloId, Guid localId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var m = await db.ModelosInspecao.AsNoTracking().Include(x => x.Itens).FirstAsync(x => x.Id == modeloId, ct);
        if (m.Tipo != TipoModelo.SST || m.Situacao != SituacaoModelo.Vigente) throw new RegraDeNegocioException("Escolha um checklist de segurança vigente.");
        var local = await db.Locais.AsNoTracking().FirstAsync(x => x.Id == localId, ct);
        var i = new InspecaoSeguranca
        {
            ObraId = local.ObraId, LocalId = local.Id, ModeloInspecaoId = m.Id, Modelo = m.Titulo, InspetorNome = usuario.Nome, Data = Agora,
            Numero = $"IS-{await Numerador.ProximoAsync(db, "InspecaoSst", ct):0000}"
        };
        foreach (var it in m.Itens.OrderBy(x => x.Ordem))
            i.Itens.Add(new InspecaoSegurancaItem { InspecaoSegurancaId = i.Id, ItemVerificacaoId = it.Id, Ordem = it.Ordem, Descricao = it.Descricao, Referencia = it.Referencia });
        db.InspecoesSeguranca.Add(i);
        await db.SaveChangesAsync(ct);
        return i.Id;
    }

    /// <summary>Salva/conclui a inspeção. NC alta ou risco grave abre RNC (origem inspeção de segurança).</summary>
    public async Task SalvarInspecaoAsync(InspecaoSeguranca dados, bool concluir, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var i = await db.InspecoesSeguranca.Include(x => x.Itens).FirstAsync(x => x.Id == dados.Id, ct);
        foreach (var it in i.Itens)
        {
            var n = dados.Itens.First(x => x.Id == it.Id);
            it.Resultado = n.Resultado; it.Gravidade = n.Gravidade; it.Responsavel = n.Responsavel; it.Prazo = n.Prazo; it.Observacao = n.Observacao; it.Corrigido = n.Corrigido;
        }
        i.Paralisacao = dados.Paralisacao;
        if (concluir)
        {
            ValidacaoException.Se(i.Concluir());
            foreach (var it in i.Itens.Where(x => x.Resultado == ResultadoItem.NaoConforme && x.Gravidade >= Gravidade.Alta && x.RncId is null))
            {
                var r = new Rnc
                {
                    Numero = $"RNC-{await Numerador.ProximoAsync(db, "Rnc", ct):0000}", Origem = OrigemRnc.InspecaoSeguranca, Referencia = $"{i.Numero} · item {it.Ordem}",
                    ReferenciaId = i.Id, ObraId = i.ObraId, LocalId = i.LocalId, Classificacao = ClassificacaoRnc.Seguranca, Gravidade = it.Gravidade!.Value, ExigeAcao = true,
                    Descricao = $"{it.Descricao} ({it.Referencia}): {it.Observacao}", Disposicao = it.Gravidade == Gravidade.RiscoGraveIminente ? "Paralisação da frente até correção" : "Correção imediata",
                    ResponsavelNome = it.Responsavel, Prazo = it.Prazo ?? Hoje, Status = StatusRnc.EmTratamento
                };
                db.Rncs.Add(r); it.RncId = r.Id;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── Permissão de trabalho (S11/S12) ─────────

    public async Task<Guid> SalvarPtAsync(PermissaoTrabalho pt, CancellationToken ct = default)
    {
        var e = new List<string>();
        if (pt.LocalId == Guid.Empty) e.Add("Informe o local.");
        if (string.IsNullOrWhiteSpace(pt.Atividade)) e.Add("Informe a atividade.");
        if (pt.FornecedorId == Guid.Empty) e.Add("Informe a empresa.");
        ValidacaoException.Se(e);
        await using var db = fabrica.Criar();
        if (string.IsNullOrEmpty(pt.Numero)) pt.Numero = $"PT-{await Numerador.ProximoAsync(db, "Pt", ct):0000}";
        if (string.IsNullOrWhiteSpace(pt.Medidas)) pt.Medidas = PermissaoTrabalho.MedidasPadrao(pt.Tipo);
        var cols = await db.Colaboradores.AsNoTracking().Where(c => pt.Executantes.Select(x => x.ColaboradorId).Contains(c.Id)).ToListAsync(ct);
        foreach (var x in pt.Executantes)
        {
            x.PermissaoTrabalhoId = pt.Id;
            var c = cols.First(c => c.Id == x.ColaboradorId);
            x.Nome = c.Nome;
            (x.Liberado, x.Motivo) = RegrasSst.PodeExecutar(c, pt.Tipo);
        }
        if (pt.Status == StatusPermissaoTrabalho.Rascunho && pt.Executantes.Count > 0) pt.Status = StatusPermissaoTrabalho.AguardandoLiberacao;
        await Grafo.SalvarAsync(db, pt, nameof(PermissaoTrabalho.Executantes));
        await db.SaveChangesAsync(ct);
        return pt.Id;
    }

    public async Task LiberarPtAsync(PermissaoTrabalho dados, CancellationToken ct = default)
    {
        await SalvarPtAsync(dados, ct);
        await using var db = fabrica.Criar();
        var pt = await db.PermissoesTrabalho.Include(x => x.Executantes).FirstAsync(x => x.Id == dados.Id, ct);
        ValidacaoException.Se(pt.Liberar(Agora));
        await db.SaveChangesAsync(ct);
    }

    public async Task MudarStatusPtAsync(Guid id, StatusPermissaoTrabalho status, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var pt = await db.PermissoesTrabalho.FirstAsync(x => x.Id == id, ct);
        if (status == StatusPermissaoTrabalho.Encerrada) { pt.EncerradaEm = Agora; pt.EncerradaPor = usuario.Nome; }
        pt.Status = status;
        await db.SaveChangesAsync(ct);
    }

    // ───────── Ocorrências (S13) ─────────

    public async Task<Guid> SalvarOcorrenciaAsync(Ocorrencia o, CancellationToken ct = default)
    {
        ValidacaoException.Se(o.Validar());
        await using var db = fabrica.Criar();
        if (string.IsNullOrEmpty(o.Numero)) o.Numero = $"OC-{await Numerador.ProximoAsync(db, "Ocorrencia", ct):0000}";
        await Grafo.SalvarAsync(db, o);
        await db.SaveChangesAsync(ct);
        return o.Id;
    }
}
