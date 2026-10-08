using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Common;
using QualyHub.Domain.Documentos;
using QualyHub.Domain.Regras;

namespace QualyHub.Application.Servicos;

/// <summary>Permissão efetiva do usuário logado sobre um documento (D07). Sem regra: Diretoria/Qualidade/Admin tudo; demais só ver vigentes.</summary>
public sealed record PermissaoEfetiva(bool Ver, bool Baixar, bool Imprimir, bool Editar, bool Aprovar, bool VerObsoletos);

/// <summary>Casos de uso do controle de documentos do SGQ (D01–D13), com log permanente de eventos (D09).</summary>
public sealed class DocumentosServico(IFabricaDb fabrica, IUsuarioAtual usuario, IArmazenamento armazenamento, TimeProvider relogio)
{
    private DateTimeOffset Agora => relogio.GetUtcNow();
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetLocalNow().Date);

    public async Task<PermissaoEfetiva> PermissaoAsync(IQualyHubDb db, Documento doc, CancellationToken ct = default)
    {
        var perfil = await PerfilAsync(db, ct);
        var regras = await db.Permissoes.AsNoTracking().Where(p => p.AlvoId == doc.Id || p.AlvoId == doc.PastaId).ToListAsync(ct);
        var r = RegrasDocumentos.Efetiva(regras, doc.PastaId, doc.Id, usuario.UsuarioId ?? Guid.Empty, perfil, Hoje);
        if (r is not null) return new(r.PodeVer, r.PodeBaixar, r.PodeImprimir, r.PodeEditar, r.PodeAprovar, r.PodeVerObsoletos);
        var gestor = perfil is PerfilUsuario.Administrador or PerfilUsuario.Diretoria or PerfilUsuario.Qualidade;
        return new(true, gestor, gestor, gestor || doc.ResponsavelUsuarioId == usuario.UsuarioId, gestor, perfil is PerfilUsuario.Qualidade or PerfilUsuario.Administrador);
    }

    private async Task<PerfilUsuario> PerfilAsync(IQualyHubDb db, CancellationToken ct) =>
        usuario.UsuarioId is { } id ? await db.Usuarios.Where(u => u.Id == id).Select(u => u.Perfil).FirstOrDefaultAsync(ct) : PerfilUsuario.Administrador;

    public void Log(IQualyHubDb db, EventoDocumentoTipo evento, Guid? documentoId, Guid? revisaoId = null, Guid? copiaId = null, string? detalhe = null, string? referencia = null) =>
        db.EventosDocumento.Add(new EventoDocumento
        {
            EmpresaId = usuario.EmpresaId, DataHora = Agora, UsuarioId = usuario.UsuarioId, UsuarioNome = usuario.Nome, Evento = evento,
            DocumentoId = documentoId, RevisaoId = revisaoId, CopiaId = copiaId, Detalhe = detalhe, Referencia = referencia, Origem = usuario.Origem
        });

    // ───────── Pastas (processo/área) ─────────

    public async Task<Guid> CriarPastaAsync(Pasta p, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarPasta(p));
        await using var db = fabrica.Criar();
        // A tela só mostra "Nova pasta" para a gestão do SGQ; a regra vale também fora dela.
        await Permissoes.ExigirAsync(db, usuario, "criar pastas do SGQ", PerfilUsuario.Qualidade, PerfilUsuario.Diretoria);
        // Cópia limpa: a instância vem da tela e não deve arrastar estado de rastreamento para este contexto.
        var nova = new Pasta { Id = p.Id, Nome = p.Nome.Trim(), Ordem = p.Ordem, PastaPaiId = p.PastaPaiId };
        db.Pastas.Add(nova);
        await db.SaveChangesAsync(ct);
        return nova.Id;
    }

    public static IReadOnlyList<string> ValidarPasta(Pasta p) =>
        string.IsNullOrWhiteSpace(p.Nome) ? ["Informe o nome."] : [];

    /// <summary>D09: abrir a ficha do documento conta como acesso e fica no log permanente.</summary>
    public async Task RegistrarVisualizacaoAsync(Guid documentoId, Guid? revisaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        Log(db, EventoDocumentoTipo.Visualizacao, documentoId, revisaoId);
        await db.SaveChangesAsync(ct);
    }

    // ───────── Documentos externos ─────────

    public async Task SalvarExternoAsync(DocumentoExterno e, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarExterno(e));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, e);
        await db.SaveChangesAsync(ct);
    }

    public static IReadOnlyList<string> ValidarExterno(DocumentoExterno d)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Identificacao)) e.Add("Informe a identificação.");
        if (string.IsNullOrWhiteSpace(d.Titulo)) e.Add("Informe o título.");
        if (string.IsNullOrWhiteSpace(d.Edicao)) e.Add("Informe a edição.");
        if (string.IsNullOrWhiteSpace(d.Emissor)) e.Add("Informe o emissor.");
        return e;
    }

    // ───────── Controle de registros (retenção) ─────────

    public async Task SalvarRegraRetencaoAsync(RegraRetencao r, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarRegraRetencao(r));
        await using var db = fabrica.Criar();
        await Grafo.SalvarAsync(db, r);
        await db.SaveChangesAsync(ct);
    }

    public static IReadOnlyList<string> ValidarRegraRetencao(RegraRetencao r)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(r.Identificacao)) e.Add("Informe o registro.");
        if (string.IsNullOrWhiteSpace(r.Armazenamento) || string.IsNullOrWhiteSpace(r.Recuperacao) || string.IsNullOrWhiteSpace(r.Protecao)
            || string.IsNullOrWhiteSpace(r.TempoRetencao) || string.IsNullOrWhiteSpace(r.Descarte))
            e.Add("Preencha armazenamento, recuperação, proteção, retenção e descarte.");
        return e;
    }

    // ───────── D01/D02 ─────────

    public async Task<Guid> CriarDocumentoAsync(Documento d, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Codigo)) e.Add("Informe o código.");
        if (string.IsNullOrWhiteSpace(d.Titulo)) e.Add("Informe o título.");
        if (d.PastaId == Guid.Empty) e.Add("Informe o processo/área (pasta).");
        if (await db.Documentos.AnyAsync(x => x.Codigo == d.Codigo, ct)) e.Add($"Código {d.Codigo} já existe.");
        ValidacaoException.Se(e);
        if (d.PeriodicidadeMeses <= 0) d.PeriodicidadeMeses = (await ParametrosServico.CarregarAsync(db, ct)).Int(Domain.Estrutura.Parametros.PeriodicidadeAnaliseCriticaMeses);
        db.Documentos.Add(d);
        var rev = d.NovaRevisao(MotivoRevisao.EmissaoInicial, null, usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora);
        Log(db, EventoDocumentoTipo.Criacao, d.Id, rev.Id, detalhe: $"{d.Codigo} – {d.Titulo}");
        await db.SaveChangesAsync(ct);
        return d.Id;
    }

    public async Task<Guid> NovaRevisaoAsync(Guid documentoId, MotivoRevisao motivo, string descricao, string? origem, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var d = await db.Documentos.Include(x => x.Revisoes).FirstAsync(x => x.Id == documentoId, ct);
        var rev = d.NovaRevisao(motivo, descricao, usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora);
        rev.OrigemRevisao = origem;
        Log(db, EventoDocumentoTipo.NovaRevisao, d.Id, rev.Id, detalhe: $"{rev.Rotulo}: {descricao}");
        await db.SaveChangesAsync(ct);
        return rev.Id;
    }

    /// <summary>Anexa o arquivo editável (.docx/.xlsx) ou publicado (PDF, com hash). Nome gerado pelo sistema: &lt;CÓDIGO&gt; - &lt;TÍTULO&gt; R&lt;nn&gt;.</summary>
    public async Task AnexarArquivoAsync(Guid revisaoId, Stream conteudo, string nomeOriginal, bool publicado, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var rev = await db.Revisoes.Include(r => r.Documento).FirstAsync(r => r.Id == revisaoId, ct);
        if (rev.Status is StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas or StatusRevisao.Obsoleta)
            throw new RegraDeNegocioException("Revisão publicada é imutável: abra nova revisão.");
        var ext = Path.GetExtension(nomeOriginal).TrimStart('.').ToLowerInvariant();
        if (publicado && ext != "pdf") throw new RegraDeNegocioException("O arquivo publicado deve ser PDF.");
        var salvo = await armazenamento.SalvarAsync(conteudo, nomeOriginal, "sgq", publicado ? "application/pdf" : null, ct);
        var nome = Documento.NomeArquivo(rev.Documento.Codigo, rev.Documento.Titulo, rev.Revisao, ext);
        if (publicado) { rev.ArquivoPublicadoBlob = salvo.Blob; rev.ArquivoPublicadoNome = nome; rev.HashSha256 = salvo.Hash; }
        else { rev.ArquivoEditavelBlob = salvo.Blob; rev.ArquivoEditavelNome = nome; }
        Log(db, EventoDocumentoTipo.ArquivoMovido, rev.DocumentoId, rev.Id, detalhe: $"{(publicado ? "Publicado" : "Editável")}: {nome} (sha256 {salvo.Hash[..12]}…)");
        await db.SaveChangesAsync(ct);
    }

    public async Task DecidirEtapaAsync(Guid revisaoId, DecisaoEtapa decisao, string? comentario, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var rev = await db.Revisoes.Include(r => r.Etapas).Include(r => r.Documento).FirstAsync(r => r.Id == revisaoId, ct);
        var perm = await PermissaoAsync(db, rev.Documento, ct);
        var etapa = rev.EtapaAtual?.Etapa;
        if (etapa == EtapaRevisaoTipo.Publicacao) throw new RegraDeNegocioException("Use 'Publicar' para concluir a última etapa.");
        rev.Decidir(usuario.UsuarioId ?? Guid.Empty, usuario.Nome, decisao, comentario, Agora, perm.Aprovar);
        if (decisao == DecisaoEtapa.Recusado && rev.Documento.RevisaoVigenteId is null) rev.Documento.Status = StatusDocumentoSgq.EmElaboracao;
        Log(db, EventoDocumentoTipo.AprovacaoDevolucao, rev.DocumentoId, rev.Id, detalhe: $"{etapa}: {decisao}{(comentario is null ? "" : $" – {comentario}")}");
        await db.SaveChangesAsync(ct);
    }

    public async Task PublicarAsync(Guid revisaoId, DateOnly vigencia, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var rev = await db.Revisoes.Include(r => r.Etapas).FirstAsync(r => r.Id == revisaoId, ct);
        var doc = await db.Documentos.Include(d => d.Revisoes).ThenInclude(r => r.Copias).FirstAsync(d => d.Id == rev.DocumentoId, ct);
        var perm = await PermissaoAsync(db, doc, ct);
        var usuarios = await db.Usuarios.AsNoTracking().ToListAsync(ct);
        var p = await ParametrosServico.CarregarAsync(db, ct);
        int? anos = doc.RegraRetencaoId is { } rid ? await db.RegrasRetencao.Where(r => r.Id == rid).Select(r => r.TempoRetencaoAnos).FirstOrDefaultAsync(ct) : null;
        var res = RegrasDocumentos.Publicar(doc, rev, vigencia, usuarios, p, anos, usuario.UsuarioId ?? Guid.Empty, usuario.Nome, Agora, perm.Aprovar);
        db.Ciencias.AddRange(res.Ciencias);
        Log(db, EventoDocumentoTipo.Publicacao, doc.Id, rev.Id, detalhe: $"{doc.Codigo} {rev.Rotulo} vigente a partir de {vigencia:dd/MM/yyyy}");
        if (res.Anterior is not null) await QualidadeObraServico.DocumentoRevisadoAsync(db, doc.Id, ct);
        if (res.Anterior is { } ant)
            Log(db, EventoDocumentoTipo.Obsolescencia, doc.Id, ant.Id, detalhe: $"{ant.Rotulo} obsoleta · {res.CopiasParaRecolher.Count} cópia(s) aguardando recolhimento");
        await db.SaveChangesAsync(ct);
    }

    // ───────── D08 Check-out ─────────

    public async Task CheckoutAsync(Guid revisaoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var rev = await db.Revisoes.Include(r => r.Documento).FirstAsync(r => r.Id == revisaoId, ct);
        if (!(await PermissaoAsync(db, rev.Documento, ct)).Editar) throw new RegraDeNegocioException("Sem permissão de edição.");
        var existentes = await db.Checkouts.Where(c => c.DocumentoRevisaoId == revisaoId).ToListAsync(ct);
        var c = RegrasDocumentos.Checkout(rev, existentes, usuario.UsuarioId ?? Guid.Empty, usuario.Nome, await ParametrosServico.CarregarAsync(db, ct), Agora);
        if (!existentes.Contains(c)) { db.Checkouts.Add(c); Log(db, EventoDocumentoTipo.CheckoutCheckin, rev.DocumentoId, rev.Id, detalhe: "Check-out"); }
        await db.SaveChangesAsync(ct);
    }

    public async Task CheckinAsync(Guid revisaoId, bool forcado, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Checkouts.FirstOrDefaultAsync(x => x.DocumentoRevisaoId == revisaoId && x.CheckinEm == null && x.LiberadoForcadoPor == null, ct)
                ?? throw new RegraDeNegocioException("Não há check-out aberto.");
        var rev = await db.Revisoes.FirstAsync(r => r.Id == revisaoId, ct);
        if (forcado)
        {
            if (await PerfilAsync(db, ct) is not (PerfilUsuario.Qualidade or PerfilUsuario.Administrador))
                throw new RegraDeNegocioException("Liberação forçada é exclusiva da Qualidade.");
            c.LiberadoForcadoPor = usuario.Nome;
            Log(db, EventoDocumentoTipo.LiberacaoForcadaCheckout, rev.DocumentoId, rev.Id, detalhe: $"Check-out de {c.UsuarioNome} liberado");
        }
        else
        {
            if (c.UsuarioId != usuario.UsuarioId) throw new RegraDeNegocioException($"Check-out pertence a {c.UsuarioNome}.");
            c.CheckinEm = Agora;
            Log(db, EventoDocumentoTipo.CheckoutCheckin, rev.DocumentoId, rev.Id, detalhe: "Check-in");
        }
        await db.SaveChangesAsync(ct);
    }

    // ───────── D04/D05 Cópias ─────────

    public async Task<Guid> SolicitarCopiasAsync(SolicitacaoCopia s, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (s.Itens.Count == 0) throw new RegraDeNegocioException("Inclua ao menos um documento.");
        s.Numero = await Numerador.ProximoAsync(db, "SolicitacaoCopia", ct);
        s.SolicitadoPorUsuarioId = usuario.UsuarioId ?? Guid.Empty; s.SolicitadoPor = usuario.Nome; s.SolicitadoEm = Agora;
        foreach (var i in s.Itens) i.Revisao = null!;
        db.SolicitacoesCopia.Add(s);
        await db.SaveChangesAsync(ct);
        return s.Id;
    }

    public async Task LiberarSolicitacaoAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (await PerfilAsync(db, ct) is not (PerfilUsuario.Qualidade or PerfilUsuario.Administrador))
            throw new RegraDeNegocioException("Liberação de cópias é feita pela Qualidade.");
        var s = await db.SolicitacoesCopia.Include(x => x.Itens).ThenInclude(i => i.Revisao).ThenInclude(r => r.Documento).FirstAsync(x => x.Id == id, ct);
        var seqs = new Dictionary<string, Domain.Common.Sequencia>();
        var copias = RegrasDocumentos.LiberarSolicitacao(s, chave =>
        {
            if (!seqs.TryGetValue(chave, out var seq))
            {
                seq = db.Sequencias.FirstOrDefault(x => x.Chave == chave) ?? db.Sequencias.Add(new Domain.Common.Sequencia { EmpresaId = usuario.EmpresaId, Chave = chave }).Entity;
                seqs[chave] = seq;
            }
            return seq.Proximo();
        }, usuario.Nome, Agora);
        db.Copias.AddRange(copias);
        foreach (var c in copias) Log(db, EventoDocumentoTipo.CopiaDistribuida, c.Revisao.DocumentoId, c.DocumentoRevisaoId, c.Id, $"{c.NumeroCopia} → {c.Destinatario}");
        await db.SaveChangesAsync(ct);
    }

    public async Task RegistrarEntregaAsync(Guid copiaId, string recebidoPor, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Copias.Include(x => x.Revisao).FirstAsync(x => x.Id == copiaId, ct);
        c.EntregueEm = Agora; c.RecebidoPor = recebidoPor;
        Log(db, EventoDocumentoTipo.CopiaDistribuida, c.Revisao.DocumentoId, c.DocumentoRevisaoId, c.Id, $"{c.NumeroCopia} entregue a {recebidoPor}");
        await db.SaveChangesAsync(ct);
    }

    public async Task RecolherCopiaAsync(Guid copiaId, DestinoObsoleta destino, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Copias.Include(x => x.Revisao).ThenInclude(r => r.Copias).FirstAsync(x => x.Id == copiaId, ct);
        c.Recolher(usuario.Nome, destino, Agora);
        c.Revisao.RecolhimentoOk = c.Revisao.Copias.All(x => x.Status != StatusCopia.RecolhimentoPendente || x.TipoCopia != TipoCopia.FisicaControlada);
        Log(db, EventoDocumentoTipo.CopiaRecolhida, c.Revisao.DocumentoId, c.DocumentoRevisaoId, c.Id, $"{c.NumeroCopia} recolhida ({destino})");
        await db.SaveChangesAsync(ct);
    }

    // ───────── D06 Ciência ─────────

    public async Task ConfirmarLeituraAsync(Guid cienciaId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var c = await db.Ciencias.Include(x => x.Revisao).FirstAsync(x => x.Id == cienciaId, ct);
        if (c.UsuarioId != usuario.UsuarioId) throw new RegraDeNegocioException("A confirmação de leitura é pessoal.");
        c.LidoEm ??= Agora;
        Log(db, EventoDocumentoTipo.LeituraConfirmada, c.Revisao.DocumentoId, c.DocumentoRevisaoId, detalhe: "Li e entendi");
        await db.SaveChangesAsync(ct);
    }

    // ───────── D12 GRD ─────────

    public async Task<Guid> EnviarGrdAsync(Grd g, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        if (g.Itens.Count == 0) throw new RegraDeNegocioException("Inclua ao menos um documento.");
        var revs = await db.Revisoes.Include(r => r.Documento).Where(r => g.Itens.Select(i => i.DocumentoRevisaoId).Contains(r.Id)).ToListAsync(ct);
        if (revs.Any(r => r.Status is not (StatusRevisao.Vigente or StatusRevisao.VigenteComRessalvas)))
            throw new RegraDeNegocioException("GRD só envia revisões vigentes.");
        g.Numero = $"GRD-{Hoje.Year}-{await Numerador.ProximoAsync(db, $"GRD-{Hoje.Year}", ct):000}";
        g.EnviadoEm = Agora; g.EnviadoPor = usuario.Nome;
        foreach (var i in g.Itens) i.Revisao = null!;
        db.Grds.Add(g);
        var fornecedor = await db.Fornecedores.Where(f => f.Id == g.FornecedorId).Select(f => f.RazaoSocial).FirstAsync(ct);
        foreach (var r in revs)
        {
            var n = await Numerador.ProximoAsync(db, $"Copia-{r.Documento.Codigo}-R{r.Revisao:00}", ct);
            var c = new CopiaControlada
            {
                NumeroCopia = CopiaControlada.Numerar(r.Documento.Codigo, r.Revisao, n), DocumentoRevisaoId = r.Id, TipoCopia = TipoCopia.ExternaGrd,
                Destinatario = fornecedor, GrdId = g.Id
            };
            db.Copias.Add(c);
            Log(db, EventoDocumentoTipo.CopiaDistribuida, r.DocumentoId, r.Id, c.Id, $"{c.NumeroCopia} via {g.Numero} → {fornecedor}");
        }
        await db.SaveChangesAsync(ct);
        return g.Id;
    }

    /// <summary>Confirmação de recebimento pelo link da GRD (acesso externo, sem login).</summary>
    public static async Task<bool> ConfirmarGrdAsync(IQualyHubDb db, Guid token, DateTimeOffset agora, CancellationToken ct = default)
    {
        var g = await db.Grds.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TokenConfirmacao == token && !x.Excluido, ct);
        if (g is null) return false;
        g.ConfirmadoEm ??= agora;
        var copias = await db.Copias.IgnoreQueryFilters().Where(c => c.GrdId == g.Id).ToListAsync(ct);
        foreach (var c in copias) { c.EntregueEm ??= agora; c.RecebidoPor ??= "Confirmado pelo link da GRD"; }
        await db.SaveChangesAsync(ct);
        return true;
    }
}
