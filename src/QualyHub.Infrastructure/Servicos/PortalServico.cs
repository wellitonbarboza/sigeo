using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Aprovacoes;
using QualyHub.Domain.Cadastros;
using QualyHub.Domain.Common;
using QualyHub.Domain.Regras;
using QualyHub.Domain.Suprimentos;
using QualyHub.Infrastructure.Contexto;
using QualyHub.Infrastructure.Dados;

namespace QualyHub.Infrastructure.Servicos;

/// <summary>Contextos com identidade de sistema (portal externo, GRD, carga inicial, rotinas) — sempre auditados.</summary>
public sealed class FabricaSistema(string connectionString)
{
    public QualyHubDbContext Criar(Guid empresaId, string origem)
    {
        var usuario = new UsuarioSistema(empresaId, origem);
        var opts = new DbContextOptionsBuilder<QualyHubDbContext>()
            .UseSqlServer(connectionString, s => s.MigrationsHistoryTable("__Migracoes", "qh"))
            .AddInterceptors(new AuditoriaInterceptor(usuario, TimeProvider.System))
            .Options;
        return new QualyHubDbContext(opts, usuario);
    }
}

/// <summary>Documento pedido ao fornecedor no portal (tela 24).</summary>
public sealed record DocumentoSolicitado(Guid TipoId, string Nome, bool PorEntrega, bool Obrigatorio, StatusDocumento Situacao, DateOnly? Validade, string? Detalhe);

public sealed record SessaoPortal(Guid EmpresaId, string EmpresaNome, Fornecedor Fornecedor, LinkPortalFornecedor Link, IReadOnlyList<DocumentoSolicitado> Documentos, string? Alerta)
{
    public IReadOnlyList<ColaboradorPortal> Colaboradores { get; init; } = [];
    public IReadOnlyList<(Guid Id, string Nome)> Exigencias { get; init; } = [];
}

/// <summary>Proposta de cotação preenchida pelo fornecedor no portal (link da cotação, sem login).</summary>
public sealed record PropostaPortal(string EmpresaNome, string FornecedorNome, int CotacaoNumero, string CotacaoNome, string? EnderecoEntrega,
    TipoFrete Frete, bool Aberta, CotacaoFornecedor Convite, IReadOnlyList<CotacaoItem> Itens, string? FaturarPara = null);

public sealed record ItemPropostaPortal(Guid ItemId, decimal ValorUnitario, DateOnly? Entrega);

/// <summary>Funcionário da empresa terceira visto no portal (aba Funcionários): só nome, função e pendências — sem CPF.</summary>
public sealed record ColaboradorPortal(Guid Id, string Nome, string Funcao, string Situacao, string? Pendencias);

/// <summary>
/// Portal do fornecedor: acesso externo por link com token (sem login), prazo e registro de IP.
/// O documento enviado entra "Em validação" — Suprimentos aprova ou recusa com motivo.
/// </summary>
public sealed class PortalServico(FabricaSistema sistema, FabricaDb fabrica, IArmazenamento armazenamento, IUsuarioAtual usuario, TimeProvider relogio)
{
    public async Task<(string Token, string Codigo)> GerarLinkAsync(Guid fornecedorId, int dias, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var token = Token.Novo();
        var codigo = $"SOL-{await Numerador.ProximoAsync(db, "LinkPortal", ct):0000}";
        var f = await db.Fornecedores.AsNoTracking().FirstAsync(x => x.Id == fornecedorId, ct);
        var motivo = await db.LinksPortal.AnyAsync(l => l.FornecedorId == fornecedorId, ct) ? "Renovação" : "Cadastro inicial";
        db.LinksPortal.Add(new LinkPortalFornecedor
        {
            FornecedorId = fornecedorId, Codigo = codigo, TokenHash = Token.Hash(token), ValidoAte = relogio.GetUtcNow().AddDays(dias),
            EmailDestino = f.Emails, Motivo = motivo, EnviadoPor = usuario.Nome, Status = StatusSolicitacaoDoc.Enviada
        });
        await db.SaveChangesAsync(ct);
        return (token, codigo);
    }

    public async Task<SessaoPortal?> AbrirAsync(string token, string? ip, CancellationToken ct = default)
    {
        var hash = Token.Hash(token);
        Guid empresaId;
        await using (var busca = sistema.Criar(Guid.Empty, "Portal"))
        {
            var l = await busca.LinksPortal.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (l is null || !l.Valido(relogio.GetUtcNow())) return null;
            empresaId = l.EmpresaId;
        }
        await using var db = sistema.Criar(empresaId, "Portal");
        var link = await db.LinksPortal.FirstAsync(x => x.TokenHash == hash, ct);
        link.UltimoAcessoEm = relogio.GetUtcNow();
        if (link.Status == StatusSolicitacaoDoc.Enviada) link.Status = StatusSolicitacaoDoc.Acessada;
        link.UltimoAcessoIp = ip;
        await db.SaveChangesAsync(ct);

        var f = await db.Fornecedores.AsNoTracking().Include(x => x.Documentos).ThenInclude(d => d.TipoDocumento).FirstAsync(x => x.Id == link.FornecedorId, ct);
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor && t.Ativo).ToListAsync(ct);
        var empresa = await db.Empresas.AsNoTracking().Where(e => e.Id == empresaId).Select(e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial).FirstAsync(ct);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);

        var docs = tipos.Where(t => t.ExigidoPara == TipoFornecimento.Nenhum || (t.ExigidoPara & f.TipoFornecimento) != 0)
            .Select(t =>
            {
                var d = f.Documentos.Where(x => x.TipoDocumentoExigidoId == t.Id && !x.Excluido).OrderByDescending(x => x.CriadoEm).FirstOrDefault();
                var sit = t.PorEntrega ? StatusDocumento.Valido : d is null ? StatusDocumento.PendenteEnvio : StatusDocumentoCalculo.Calcular(d, hoje);
                var det = t.PorEntrega ? "Enviar junto com a NF" : d?.Status == StatusDocumento.Reprovado ? $"Recusado: {d.MotivoRecusa}"
                        : d?.Status == StatusDocumento.EmValidacao ? $"Enviado {d.CriadoEm.ToLocalTime():dd/MM HH:mm}"
                        : d?.DataValidade is { } v ? $"Validade {v:dd/MM/yyyy}" : t.Obrigatorio ? "Obrigatório" : "Opcional";
                return new DocumentoSolicitado(t.Id, t.Nome, t.PorEntrega, t.Obrigatorio, sit, d?.DataValidade, det);
            }).OrderBy(d => d.Situacao == StatusDocumento.Valido).ThenBy(d => d.Nome).ToList();

        var vence = docs.Where(d => d.Validade is { } v && v >= hoje && v <= hoje.AddDays(30)).OrderBy(d => d.Validade).FirstOrDefault();
        var alerta = vence is null ? null : $"Seu documento '{vence.Nome}' vence em {vence.Validade:dd/MM/yyyy}. Sem documento válido, novos pedidos ficam bloqueados.";
        var funcoes = await db.Funcoes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Descricao, ct);
        var cols = await db.Colaboradores.AsNoTracking().Where(c => c.FornecedorId == f.Id && (c.DataSaida == null || c.DataSaida > hoje)).OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.FuncaoObraId, c.Situacao, c.Pendencias }).ToListAsync(ct);
        var exig = await db.Exigencias.AsNoTracking().Where(x => x.Tipo != Domain.Common.TipoExigencia.Epi).OrderBy(x => x.Ordem).Select(x => new { x.Id, x.Descricao }).ToListAsync(ct);
        return new SessaoPortal(empresaId, empresa, f, link, docs, alerta)
        {
            Colaboradores = cols.Select(c => new ColaboradorPortal(c.Id, c.Nome, funcoes.GetValueOrDefault(c.FuncaoObraId, ""), c.Situacao.ToString(), c.Pendencias)).ToList(),
            Exigencias = exig.Select(x => (x.Id, x.Descricao)).ToList()
        };
    }

    public async Task ReceberAsync(string token, Guid tipoId, string? numero, DateOnly? emissao, DateOnly? validade, string responsavel,
        Stream arquivo, string nomeArquivo, string? contentType, CancellationToken ct = default)
    {
        var sessao = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Link inválido ou expirado.");
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png")) throw new RegraDeNegocioException("Envie PDF, JPG ou PNG.");
        if (string.IsNullOrWhiteSpace(responsavel)) throw new RegraDeNegocioException("Informe o responsável pelo envio.");
        var salvo = await armazenamento.SalvarAsync(arquivo, nomeArquivo, "fornecedores", contentType, ct);
        await using var db = sistema.Criar(sessao.EmpresaId, "Portal");
        db.FornecedorDocumentos.Add(new FornecedorDocumento
        {
            EmpresaId = sessao.EmpresaId, FornecedorId = sessao.Fornecedor.Id, TipoDocumentoExigidoId = tipoId, Numero = numero, DataEmissao = emissao,
            DataValidade = validade, ArquivoBlob = salvo.Blob, ArquivoNomeOriginal = nomeArquivo, ArquivoHashSha256 = salvo.Hash,
            Status = StatusDocumento.EmValidacao, EnviadoPeloPortal = true, ResponsavelEnvio = responsavel,
            Bloqueia = await db.TiposDocumento.Where(t => t.Id == tipoId).Select(t => t.BloqueiaQuandoVencido).FirstAsync(ct)
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Aba Funcionários: a empresa envia documento por pessoa; só vale após validação do SST.</summary>
    public async Task ReceberDocColaboradorAsync(string token, Guid colaboradorId, Guid exigenciaId, DateOnly emissao, DateOnly? validade, string responsavel,
        Stream arquivo, string nomeArquivo, string? contentType, CancellationToken ct = default)
    {
        var sessao = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Link inválido ou expirado.");
        if (sessao.Colaboradores.All(c => c.Id != colaboradorId)) throw new RegraDeNegocioException("Funcionário não pertence a esta empresa.");
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png")) throw new RegraDeNegocioException("Envie PDF, JPG ou PNG.");
        if (string.IsNullOrWhiteSpace(responsavel)) throw new RegraDeNegocioException("Informe o responsável pelo envio.");
        await using var db = sistema.Criar(sessao.EmpresaId, "Portal");
        var ex = await db.Exigencias.AsNoTracking().FirstAsync(x => x.Id == exigenciaId, ct);
        var salvo = await armazenamento.SalvarAsync(arquivo, nomeArquivo, ex.Tipo == Domain.Common.TipoExigencia.ExameAso ? "sst-restrito" : "sst", contentType, ct);
        db.DocumentosColaborador.Add(new Domain.Sst.DocumentoColaborador
        {
            EmpresaId = sessao.EmpresaId, ColaboradorId = colaboradorId, ExigenciaId = exigenciaId, DataEmissao = emissao, Validade = validade,
            ArquivoBlob = salvo.Blob, ArquivoNome = nomeArquivo, ArquivoRestrito = ex.Tipo == Domain.Common.TipoExigencia.ExameAso,
            Origem = Domain.Common.OrigemDocColaborador.PortalFornecedor, Validacao = Domain.Common.ValidacaoDoc.Aguardando, ValidadoPor = $"enviado por {responsavel}"
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Lançamento interno (Suprimentos/Qualidade) de documento do fornecedor, já conferido.</summary>
    public async Task<Guid> RegistrarDocumentoInternoAsync(FornecedorDocumento d, Stream? arquivo, string? nomeArquivo, string? contentType, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var tipo = await db.TiposDocumento.FirstOrDefaultAsync(t => t.Id == d.TipoDocumentoExigidoId, ct)
                   ?? throw new RegraDeNegocioException("Informe o tipo de documento.");
        if (d.FornecedorId == Guid.Empty) throw new RegraDeNegocioException("Informe o fornecedor.");
        if (d.DataEmissao is { } e && d.DataValidade is { } v && v < e) throw new RegraDeNegocioException("Validade anterior à emissão.");
        var existente = await db.FornecedorDocumentos.FirstOrDefaultAsync(x => x.Id == d.Id, ct);
        var alvo = existente ?? d;
        if (existente is not null)
        {
            existente.Numero = d.Numero; existente.DataEmissao = d.DataEmissao; existente.DataValidade = d.DataValidade;
            existente.Bloqueia = d.Bloqueia; existente.Observacao = d.Observacao; existente.Conferencia = d.Conferencia;
        }
        if (arquivo is not null && nomeArquivo is not null)
        {
            var salvo = await armazenamento.SalvarAsync(arquivo, nomeArquivo, "fornecedores", contentType, ct);
            alvo.ArquivoBlob = salvo.Blob; alvo.ArquivoNomeOriginal = nomeArquivo; alvo.ArquivoHashSha256 = salvo.Hash;
        }
        alvo.ValidadoPorUsuarioId = usuario.UsuarioId; alvo.ValidadoEm = relogio.GetUtcNow(); alvo.MotivoRecusa = null;
        alvo.Status = StatusDocumentoCalculo.Calcular(alvo.DataValidade, alvo.ArquivoBlob is not null, tipo.DiasAlertaAntesVencimento, DateOnly.FromDateTime(relogio.GetLocalNow().Date));
        if (existente is null) { d.TipoDocumento = null!; d.Fornecedor = null!; db.FornecedorDocumentos.Add(d); }
        await db.SaveChangesAsync(ct);
        return alvo.Id;
    }

    /// <summary>Suprimentos valida (ou recusa com motivo) o documento enviado pelo portal.</summary>
    public async Task ValidarDocumentoAsync(Guid documentoId, bool aprovado, string? motivo, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var d = await db.FornecedorDocumentos.Include(x => x.TipoDocumento).FirstAsync(x => x.Id == documentoId, ct);
        if (!aprovado && string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo da recusa.");
        d.ValidadoPorUsuarioId = usuario.UsuarioId; d.ValidadoEm = relogio.GetUtcNow();
        d.MotivoRecusa = aprovado ? null : motivo;
        d.Status = aprovado ? StatusDocumentoCalculo.Calcular(d.DataValidade, true, d.TipoDocumento.DiasAlertaAntesVencimento, DateOnly.FromDateTime(relogio.GetLocalNow().Date)) : StatusDocumento.Reprovado;
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Proposta de cotação pelo link ----------------
    private static bool CotacaoAberta(Cotacao c) => c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao;

    private async Task<(Guid EmpresaId, Guid CotacaoId)?> LocalizarConviteAsync(Guid token, CancellationToken ct)
    {
        await using var busca = sistema.Criar(Guid.Empty, "PortalCotacao");
        var r = await (from cf in busca.Set<CotacaoFornecedor>().IgnoreQueryFilters().AsNoTracking()
                       join c in busca.Cotacoes.IgnoreQueryFilters().AsNoTracking() on cf.CotacaoId equals c.Id
                       where cf.LinkResposta == token && !c.Excluido
                       select new { c.EmpresaId, c.Id }).FirstOrDefaultAsync(ct);
        return r is null ? null : (r.EmpresaId, r.Id);
    }

    public async Task<PropostaPortal?> AbrirPropostaAsync(Guid token, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) return null;
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().AsNoTracking().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        var forn = await db.Fornecedores.IgnoreQueryFilters().AsNoTracking().Where(f => f.Id == conv.FornecedorId).Select(f => f.RazaoSocial).FirstAsync(ct);
        var emp = await db.Empresas.AsNoTracking().Where(e => e.Id == loc.EmpresaId).Select(e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial).FirstAsync(ct);
        var itens = c.Itens.Where(i => i.FornecedorId == conv.FornecedorId).OrderBy(i => i.Descricao).ToList();
        var spe = c.RequisicaoId is { } rq
            ? await (from r in db.Requisicoes.IgnoreQueryFilters().AsNoTracking() join s in db.Spes.IgnoreQueryFilters().AsNoTracking() on r.SpeId equals s.Id
                     where r.Id == rq select s.RazaoSocial + " — CNPJ " + s.Cnpj).FirstOrDefaultAsync(ct)
            : null;
        return new PropostaPortal(emp, forn, c.Numero, c.Nome, c.EnderecoEntrega, c.Frete, CotacaoAberta(c) && conv.Status != StatusConvite.Declinou, conv, itens, spe);
    }

    public async Task EnviarPropostaAsync(Guid token, IReadOnlyList<ItemPropostaPortal> itens, decimal? frete, string? condicao, DateOnly? validade,
        DateOnly? prazoEntrega, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c)) throw new RegraDeNegocioException("Esta cotação já foi encerrada. Fale com o setor de Suprimentos.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var erros = new List<string>();
        var meus = c.Itens.Where(i => i.FornecedorId == conv.FornecedorId).ToDictionary(i => i.Id);
        foreach (var it in itens)
        {
            if (!meus.TryGetValue(it.ItemId, out var ci)) { erros.Add("Item não pertence a esta proposta."); continue; }
            if (it.ValorUnitario < 0) erros.Add($"{ci.Descricao}: valor negativo.");
            if (it.Entrega is { } e && e < hoje) erros.Add($"{ci.Descricao}: entrega no passado.");
        }
        if (!itens.Any(i => i.ValorUnitario > 0)) erros.Add("Informe o preço de pelo menos um item.");
        if (frete is < 0) erros.Add("Frete negativo.");
        if (validade is { } v && v < hoje) erros.Add("Validade da proposta no passado.");
        ValidacaoException.Se(erros);

        foreach (var it in itens)
        {
            var ci = meus[it.ItemId];
            ci.ValorOfertado = Math.Round(it.ValorUnitario, 4);
            ci.Entrega = it.Entrega ?? prazoEntrega;
        }
        conv.ValorFrete = frete; conv.CondicaoPagamento = string.IsNullOrWhiteSpace(condicao) ? null : condicao.Trim();
        conv.ValidadeProposta = validade; conv.PrazoEntregaOfertado = prazoEntrega;
        conv.Status = StatusConvite.Respondido; conv.DataResposta = hoje;
        if (c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas) c.Status = StatusCotacao.EmAnalise;
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }
}
