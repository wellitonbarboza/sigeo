using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Servicos;

/// <summary>Documento pedido ao fornecedor no portal (tela 24).</summary>
public sealed record DocumentoSolicitado(Guid TipoId, string Nome, bool PorEntrega, bool Obrigatorio, StatusDocumento Situacao, DateOnly? Validade, string? Detalhe)
{
    public FrequenciaDocumento Frequencia { get; init; } = FrequenciaDocumento.Validade;
    /// <summary>Mensal: competência esperada (mês anterior).</summary>
    public DateOnly? Competencia { get; init; }
    public string? Categoria { get; init; }
}

public sealed record SessaoPortal(Guid EmpresaId, string EmpresaNome, Fornecedor Fornecedor, LinkPortalFornecedor Link, IReadOnlyList<DocumentoSolicitado> Documentos, string? Alerta)
{
    public IReadOnlyList<ColaboradorPortal> Colaboradores { get; init; } = [];
    public IReadOnlyList<(Guid Id, string Nome)> Exigencias { get; init; } = [];
    /// <summary>Contratos de mão de obra ativos do fornecedor (obras onde os funcionários trabalham).</summary>
    public IReadOnlyList<ContratoPortal> Contratos { get; init; } = [];
    public IReadOnlyList<(Guid Id, string Nome)> Funcoes { get; init; } = [];
    public bool MaoDeObra => Contratos.Count > 0 || Fornecedor.TipoFornecimento.HasFlag(TipoFornecimento.ServicoObra);
}

public sealed record ContratoPortal(Guid Id, string Numero, Guid ObraId, string Obra);

/// <summary>Pendência mostrada no Início do portal (ordem de urgência: 1 = mais urgente).</summary>
public sealed record PendenciaPortal(int Urgencia, string Selo, string Tom, string Titulo, string Detalhe, string Aba, Guid? Id = null);
public sealed record InicioPortal(string Situacao, string Tom, string Motivo, int CotacoesAbertas, string? CotacaoUrgente, int DocsValidos, int DocsTotal, int DocsVencendo, int DocsRecusados,
    int Funcionarios, int FuncionariosPendencia, int FuncionariosAguardando, string? Competencia, int CompetenciaEntregues, int CompetenciaTotal, IReadOnlyList<PendenciaPortal> Pendencias,
    IReadOnlyList<string> EmailsAvisos);
public sealed record PedidoPortal(Guid Id, int Numero, string Obra, DateOnly Data, DateOnly Entrega, StatusPedido Status, decimal Valor, IReadOnlyList<(string Item, string Unidade, decimal Pedido, decimal Recebido)> Itens);
public sealed record VersaoDocumentoPortal(Guid Id, DateTimeOffset Enviado, string? Responsavel, DateOnly? Validade, StatusDocumento Status, string? Motivo, string? Arquivo, bool Vigente);
public sealed class ColaboradorForm
{
    public Guid? Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Cpf { get; set; }
    public Guid FuncaoId { get; set; }
    public DateOnly Admissao { get; set; }
    public Guid? ContratoId { get; set; }
    public DateOnly? Desligamento { get; set; }
}
public sealed record CompetenciaPortal(Guid Id, Guid ContratoId, string Contrato, string Obra, DateOnly Mes, SituacaoCompetencia Situacao, bool Atrasada, int Entregues, int Total, DateOnly Limite);

/// <summary>Proposta de cotação preenchida pelo fornecedor no portal (link da cotação, sem login).</summary>
public sealed record PropostaPortal(string EmpresaNome, string FornecedorNome, int CotacaoNumero, string CotacaoNome, string? EnderecoEntrega,
    TipoFrete Frete, bool Aberta, CotacaoFornecedor Convite, IReadOnlyList<CotacaoItem> Itens, string? FaturarPara = null, string? Destino = null)
{
    /// <summary>Quebra por local de cada item da proposta (torre › pavimento › apto › cômodo): item → linhas.</summary>
    public IReadOnlyDictionary<Guid, List<Comum.LocalQuantidade>> Locais { get; init; } = new Dictionary<Guid, List<Comum.LocalQuantidade>>();
    /// <summary>Cotação de material: a marca de cada item cotado é obrigatória.</summary>
    public bool Material { get; init; }
    /// <summary>Marca pedida na requisição por item da proposta (e se aceita similar).</summary>
    public IReadOnlyDictionary<Guid, (string Marca, bool Somente)> MarcasPedidas { get; init; } = new Dictionary<Guid, (string, bool)>();
    public DateTimeOffset? PrazoResposta { get; init; }
    public RascunhoProposta? Rascunho { get; init; }
    public IReadOnlyList<PropostaVersao> Versoes { get; init; } = [];
    public SituacaoPropostaFornecedor.Resultado? Situacao { get; init; }
}

public sealed record RascunhoProposta(IReadOnlyList<ItemPropostaPortal> Itens, decimal? Frete, string? Condicao, DateOnly? Validade, DateOnly? Prazo);

/// <summary>Dados cadastrais que o fornecedor completa pelo portal (pré-cadastro).</summary>
public sealed class DadosPortal
{
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Telefones { get; set; }
    public string? Emails { get; set; }
    public string? Site { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoEmail { get; set; }
    public string? ContatoTelefone { get; set; }
}

public sealed record ItemPropostaPortal(Guid ItemId, decimal ValorUnitario, DateOnly? Entrega, string? Marca = null);

/// <summary>Funcionário da empresa terceira visto no portal (aba Funcionários): só nome, função e pendências — sem CPF.</summary>
public sealed record ColaboradorPortal(Guid Id, string Nome, string Funcao, string Situacao, string? Pendencias)
{
    public string? CpfMascarado { get; init; }
    public Guid FuncaoId { get; init; }
    public DateOnly Admissao { get; init; }
    public DateOnly? Desligamento { get; init; }
    public Guid? ContratoId { get; init; }
    public string? Obra { get; init; }
    public bool AguardandoValidacao { get; init; }
}

/// <summary>
/// Portal do fornecedor: acesso externo por link com token (sem login), prazo e registro de IP.
/// O documento enviado entra "Em validação" — Suprimentos aprova ou recusa com motivo.
/// </summary>
public sealed class PortalServico(IFabricaSistema sistema, IFabricaDb fabrica, IArmazenamento armazenamento, IUsuarioAtual usuario, TimeProvider relogio, ProtecaoCpf cpf)
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

        var f = await db.Fornecedores.AsNoTracking().Include(x => x.Documentos).ThenInclude(d => d.TipoDocumento).Include(x => x.Exigencias).Include(x => x.Contatos)
                        .FirstAsync(x => x.Id == link.FornecedorId, ct);
        var tipos = await db.TiposDocumento.AsNoTracking().Where(t => t.AplicaA == AplicaA.Fornecedor && t.Ativo).OrderBy(t => t.Ordem).ThenBy(t => t.Nome).ToListAsync(ct);
        var empresa = await db.Empresas.AsNoTracking().Where(e => e.Id == empresaId).Select(e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial).FirstAsync(ct);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);

        // Documentos pedidos: os escolhidos no pré-cadastro (exigências do fornecedor) ou, sem escolha, a matriz pelo tipo de fornecimento.
        var exigF = f.Exigencias.ToDictionary(x => x.TipoDocumentoExigidoId, x => x.Obrigatorio);
        var contratos = await (from k in db.Contratos.AsNoTracking() join o in db.Obras.AsNoTracking() on k.ObraId equals o.Id
                               where k.FornecedorId == f.Id && (k.Status == StatusContrato.Ativo || k.Status == StatusContrato.AguardandoAssinaturas)
                               orderby k.Numero select new ContratoPortal(k.Id, k.Numero, k.ObraId, o.Nome)).ToListAsync(ct);
        // Mensais de contrato de mão de obra vão por competência (aba Medições e documentação mensal); de funcionário, na aba Funcionários.
        var docs = tipos.Where(t => exigF.Count > 0 ? exigF.ContainsKey(t.Id) : MatrizExigencias.Aplica(t, f.TipoFornecimento, f.IncluirDocsFuncionarios))
            .Where(t => !(t.Frequencia == FrequenciaDocumento.Mensal && contratos.Count > 0) && !(t.DeFuncionario && t.Frequencia == FrequenciaDocumento.Mensal))
            .Select(t =>
            {
                var obrig = exigF.Count > 0 ? exigF[t.Id] : t.Obrigatorio;
                var comp = t.Frequencia == FrequenciaDocumento.Mensal ? new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(-1) : (DateOnly?)null;
                var d = f.Documentos.Where(x => x.TipoDocumentoExigidoId == t.Id && !x.Excluido && (comp == null || x.Competencia == comp)).OrderByDescending(x => x.CriadoEm).FirstOrDefault();
                var sit = t.PorEntrega ? StatusDocumento.Valido : d is null ? StatusDocumento.PendenteEnvio : StatusDocumentoCalculo.Calcular(d, hoje);
                var det = t.PorEntrega ? "Enviar junto com a NF" : d?.Status == StatusDocumento.Reprovado ? $"Recusado: {d.MotivoRecusa}"
                        : d?.Status == StatusDocumento.EmValidacao ? $"Enviado {d.CriadoEm.ToLocalTime():dd/MM HH:mm}"
                        : comp is { } c ? $"Mensal — competência {c:MM/yyyy}, até o dia {t.DiaLimite ?? 20}"
                        : d?.DataValidade is { } v ? $"Validade {v:dd/MM/yyyy}" : obrig ? "Obrigatório" : "Opcional";
                return new DocumentoSolicitado(t.Id, t.Nome, t.PorEntrega, obrig, sit, d?.DataValidade, det) { Frequencia = t.Frequencia, Competencia = comp, Categoria = t.Categoria };
            }).OrderBy(d => d.Situacao == StatusDocumento.Valido).ThenBy(d => d.Categoria).ThenBy(d => d.Nome).ToList();

        var vence = docs.Where(d => d.Validade is { } v && v >= hoje && v <= hoje.AddDays(30)).OrderBy(d => d.Validade).FirstOrDefault();
        var alerta = vence is null ? null : $"Seu documento '{vence.Nome}' vence em {vence.Validade:dd/MM/yyyy}. Sem documento válido, novos pedidos ficam bloqueados.";
        var funcoes = await db.Funcoes.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Descricao, ct);
        var cols = await db.Colaboradores.AsNoTracking().Where(c => c.FornecedorId == f.Id && (c.DataSaida == null || c.DataSaida > hoje.AddDays(-60))).OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.FuncaoObraId, c.Situacao, c.Pendencias, c.CpfMascarado, c.DataInicio, c.DataSaida, c.ContratoId, c.ObraId, c.AguardandoValidacao }).ToListAsync(ct);
        var obras = await db.Obras.AsNoTracking().Select(o => new { o.Id, o.Nome }).ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        var exig = await db.Exigencias.AsNoTracking().Where(x => x.Tipo != Domain.Common.TipoExigencia.Epi).OrderBy(x => x.Ordem).Select(x => new { x.Id, x.Descricao }).ToListAsync(ct);
        return new SessaoPortal(empresaId, empresa, f, link, docs, alerta)
        {
            Colaboradores = cols.Select(c => new ColaboradorPortal(c.Id, c.Nome, funcoes.GetValueOrDefault(c.FuncaoObraId, ""),
                c.DataSaida is { } ds && ds <= hoje ? SituacaoColaborador.DesligadoDaObra.ToString() : c.Situacao.ToString(), c.Pendencias)
            {
                CpfMascarado = c.CpfMascarado, FuncaoId = c.FuncaoObraId, Admissao = c.DataInicio, Desligamento = c.DataSaida, ContratoId = c.ContratoId,
                Obra = obras.GetValueOrDefault(c.ObraId), AguardandoValidacao = c.AguardandoValidacao
            }).ToList(),
            Exigencias = exig.Select(x => (x.Id, x.Descricao)).ToList(),
            Contratos = contratos,
            Funcoes = funcoes.OrderBy(x => x.Value).Select(x => (x.Key, x.Value)).ToList()
        };
    }

    public async Task ReceberAsync(string token, Guid tipoId, string? numero, DateOnly? emissao, DateOnly? validade, string responsavel,
        Stream arquivo, string nomeArquivo, string? contentType, CancellationToken ct = default, DateOnly? competencia = null)
    {
        var sessao = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Link inválido ou expirado.");
        // Valida tudo antes de gravar o arquivo (nada de arquivo órfão no armazenamento).
        var solicitado = sessao.Documentos.FirstOrDefault(d => d.TipoId == tipoId) ?? throw new RegraDeNegocioException("Este documento não foi solicitado a este fornecedor.");
        if (string.IsNullOrWhiteSpace(responsavel)) throw new RegraDeNegocioException("Informe o responsável pelo envio.");
        if (responsavel.Length > 150 || numero?.Length > 60) throw new RegraDeNegocioException("Responsável (até 150) ou número (até 60 caracteres) muito longo.");
        if (emissao is { } em && validade is { } va && va < em) throw new RegraDeNegocioException("Validade anterior à emissão.");
        arquivo = await ArquivoPortal.ConferirAsync(arquivo, nomeArquivo, ct);
        var comp = competencia is { } cp ? new DateOnly(cp.Year, cp.Month, 1) : solicitado.Competencia;
        bool bloqueia;
        await using (var db0 = sistema.Criar(sessao.EmpresaId, "Portal"))
            bloqueia = await db0.TiposDocumento.Where(t => t.Id == tipoId).Select(t => t.BloqueiaQuandoVencido).FirstAsync(ct);
        var salvo = await armazenamento.SalvarAsync(arquivo, nomeArquivo, "fornecedores", contentType, ct);
        await using var est = sistema.Criar(sessao.EmpresaId, "Portal");
        await est.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = sistema.Criar(sessao.EmpresaId, "Portal");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // Documento mensal: reenvio da mesma competência substitui o anterior (fica no histórico).
            if (comp is { } c0)
            {
                foreach (var antigo in await db.FornecedorDocumentos.Where(x => x.FornecedorId == sessao.Fornecedor.Id && x.TipoDocumentoExigidoId == tipoId && x.Competencia == c0).ToListAsync(ct))
                    db.Remove(antigo);
                await db.SaveChangesAsync(ct);
            }
            // Nova versão nunca apaga a anterior (que continua valendo até a própria validade enquanto esta é validada).
            var anterior = comp is null ? await db.FornecedorDocumentos.AsNoTracking().Where(x => x.FornecedorId == sessao.Fornecedor.Id && x.TipoDocumentoExigidoId == tipoId)
                .OrderByDescending(x => x.CriadoEm).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct) : null;
            db.FornecedorDocumentos.Add(new FornecedorDocumento
            {
                SubstituiDocumentoId = anterior,
                EmpresaId = sessao.EmpresaId, FornecedorId = sessao.Fornecedor.Id, TipoDocumentoExigidoId = tipoId, Numero = numero?.Trim(), DataEmissao = emissao,
                DataValidade = validade, ArquivoBlob = salvo.Blob, ArquivoNomeOriginal = Path.GetFileName(nomeArquivo), ArquivoHashSha256 = salvo.Hash,
                Status = StatusDocumento.EmValidacao, EnviadoPeloPortal = true, ResponsavelEnvio = responsavel.Trim(), Competencia = comp, Bloqueia = bloqueia
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    /// <summary>Pré-cadastro: o fornecedor completa os dados cadastrais pelo link (conferidos depois por Suprimentos).</summary>
    public async Task AtualizarDadosAsync(string token, DadosPortal d, CancellationToken ct = default)
    {
        var sessao = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Link inválido ou expirado.");
        // Fornecedor já qualificado não troca e-mails/endereço pelo link (quem tem o link poderia desviar pedidos): só no pré-cadastro.
        if (!sessao.Fornecedor.PreCadastro) throw new RegraDeNegocioException("Dados cadastrais só podem ser alterados pelo link durante o pré-cadastro. Para atualizar, fale com Suprimentos.");
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Endereco) || string.IsNullOrWhiteSpace(d.Cidade) || string.IsNullOrWhiteSpace(d.Uf)) e.Add("Informe endereço, cidade e UF.");
        if (string.IsNullOrWhiteSpace(d.Emails) || !d.Emails.Contains('@')) e.Add("Informe ao menos um e-mail.");
        if (string.IsNullOrWhiteSpace(d.ContatoNome)) e.Add("Informe o nome do contato.");
        ValidacaoException.Se(e);
        await using var db = sistema.Criar(sessao.EmpresaId, "Portal");
        var f = await db.Fornecedores.Include(x => x.Contatos).FirstAsync(x => x.Id == sessao.Fornecedor.Id, ct);
        // Campo em branco mantém o valor atual (não apaga o que já existe).
        static string? T(string? v, int max = 200) => string.IsNullOrWhiteSpace(v) ? null : v.Trim() is var t && t.Length > max ? t[..max] : v.Trim();
        f.NomeFantasia = T(d.NomeFantasia) ?? f.NomeFantasia; f.InscricaoEstadual = T(d.InscricaoEstadual, 30) ?? f.InscricaoEstadual; f.InscricaoMunicipal = T(d.InscricaoMunicipal, 30) ?? f.InscricaoMunicipal;
        f.Cep = T(d.Cep, 10) ?? f.Cep; f.Endereco = T(d.Endereco) ?? f.Endereco; f.Numero = T(d.Numero, 20) ?? f.Numero; f.Complemento = T(d.Complemento) ?? f.Complemento; f.Bairro = T(d.Bairro) ?? f.Bairro;
        f.Cidade = T(d.Cidade) ?? f.Cidade; f.Uf = T(d.Uf, 2)?.ToUpperInvariant() ?? f.Uf; f.Telefones = T(d.Telefones) ?? f.Telefones; f.Emails = T(d.Emails, 400) ?? f.Emails; f.Site = T(d.Site) ?? f.Site;
        var c = f.Contatos.FirstOrDefault(x => x.Principal);
        if (c is null) { c = new FornecedorContato { FornecedorId = f.Id, Principal = true }; db.FornecedorContatos.Add(c); }
        c.Nome = T(d.ContatoNome, 150)!; c.Email = T(d.ContatoEmail) ?? c.Email; c.Telefone = T(d.ContatoTelefone, 40) ?? c.Telefone;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Aba Funcionários: a empresa envia documento por pessoa; só vale após validação do SST.</summary>
    public async Task ReceberDocColaboradorAsync(string token, Guid colaboradorId, Guid exigenciaId, DateOnly emissao, DateOnly? validade, string responsavel,
        Stream arquivo, string nomeArquivo, string? contentType, CancellationToken ct = default)
    {
        var sessao = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Link inválido ou expirado.");
        if (sessao.Colaboradores.All(c => c.Id != colaboradorId)) throw new RegraDeNegocioException("Funcionário não pertence a esta empresa.");
        if (string.IsNullOrWhiteSpace(responsavel) || responsavel.Length > 150) throw new RegraDeNegocioException("Informe o responsável pelo envio (até 150 caracteres).");
        if (sessao.Exigencias.All(x => x.Item1 != exigenciaId)) throw new RegraDeNegocioException("Exigência inválida.");
        arquivo = await ArquivoPortal.ConferirAsync(arquivo, nomeArquivo, ct);
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
        if (d.Competencia is { } cpI) d.Competencia = new DateOnly(cpI.Year, cpI.Month, 1);
        var existente = await db.FornecedorDocumentos.FirstOrDefaultAsync(x => x.Id == d.Id, ct);
        if (d.Competencia is { } c1 && await db.FornecedorDocumentos.AnyAsync(x => x.Id != d.Id && x.FornecedorId == d.FornecedorId && x.TipoDocumentoExigidoId == d.TipoDocumentoExigidoId && x.Competencia == c1, ct))
            throw new RegraDeNegocioException($"Já existe documento desta competência ({c1:MM/yyyy}). Abra o existente para corrigir.");
        var alvo = existente ?? d;
        if (existente is not null)
        {
            existente.Numero = d.Numero; existente.DataEmissao = d.DataEmissao; existente.DataValidade = d.DataValidade; existente.Competencia = d.Competencia;
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
        if (!aprovado)
        {
            var f = await db.Fornecedores.AsNoTracking().Include(x => x.Contatos).FirstAsync(x => x.Id == d.FornecedorId, ct);
            await AvisosServico.EnfileirarExternoAsync(db, Domain.Colaboracao.EventosAviso.DocumentoRecusado, AvisosServico.DestinosFornecedor(f), $"recusa:{d.Id:N}",
                new Dictionary<string, string?> { ["documento"] = d.TipoDocumento.Nome, ["motivo"] = motivo, ["assunto"] = $"Documento recusado: {d.TipoDocumento.Nome}" }, "fornecedor", ct);
        }
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Proposta de cotação pelo link ----------------
    public static bool CotacaoAberta(Cotacao c) => c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas or StatusCotacao.EmAnalise or StatusCotacao.ReprovadoVoltaCotacao;

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
        var req = c.RequisicaoId is { } rq ? await db.Requisicoes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rq, ct) : null;
        var fat = req?.EstabelecimentoFaturamentoId is { } est
            ? await db.Estabelecimentos.AsNoTracking().Where(x => x.Id == est).Select(x => x.RazaoSocial + " — CNPJ " + x.Cnpj).FirstOrDefaultAsync(ct)
            : req?.SpeId is { } sp ? await db.Spes.AsNoTracking().Where(s => s.Id == sp).Select(s => s.RazaoSocial + " — CNPJ " + s.Cnpj).FirstOrDefaultAsync(ct) : null;
        var destino = req?.SpeId is { } sp2 && req.EstabelecimentoFaturamentoId is not null
            ? await db.Spes.AsNoTracking().Where(s => s.Id == sp2).Select(s => "SPE " + (s.NomeFantasia ?? s.RazaoSocial)).FirstOrDefaultAsync(ct) + $" · requisição nº {req.Numero}"
            : null;
        var reqItens = itens.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var locs = await Comum.QuadroLocais.CarregarAsync(db, reqItens, ct);
        var porItem = itens.Where(i => i.RequisicaoItemId is { } r && locs.ContainsKey(r)).ToDictionary(i => i.Id, i => locs[i.RequisicaoItemId!.Value]);
        var pedidas = await db.Set<RequisicaoItem>().AsNoTracking().Where(r => reqItens.Contains(r.Id) && r.Marca != null).ToDictionaryAsync(r => r.Id, r => (r.Marca!, r.MesmaMarca), ct);
        var marcas = itens.Where(i => i.RequisicaoItemId is { } r && pedidas.ContainsKey(r)).ToDictionary(i => i.Id, i => pedidas[i.RequisicaoItemId!.Value]);
        var agora = relogio.GetUtcNow();
        if (conv.VisualizadoEm is null)
            await db.Set<CotacaoFornecedor>().Where(x => x.Id == conv.Id && x.VisualizadoEm == null).ExecuteUpdateAsync(u => u.SetProperty(x => x.VisualizadoEm, agora), ct);
        var versoes = await db.PropostasVersao.AsNoTracking().Where(v => v.CotacaoFornecedorId == conv.Id).OrderByDescending(v => v.Versao).ToListAsync(ct);
        conv.Versoes = versoes;
        var decidida = c.Status is StatusCotacao.AguardandoAprovacao or StatusCotacao.Aprovado or StatusCotacao.PedidoGerado;
        var venc = decidida ? (c.VencedorPorItem ? itens.Count(i => i.Vencedor) : c.FornecedorVencedorId == conv.FornecedorId ? itens.Count : 0) : 0;
        var sit = SituacaoPropostaFornecedor.Calcular(c.Status, c.PrazoResposta, conv, venc, itens.Count, decidida, agora);
        RascunhoProposta? rasc = null;
        if (!string.IsNullOrWhiteSpace(conv.RascunhoJson))
            try { rasc = System.Text.Json.JsonSerializer.Deserialize<RascunhoProposta>(conv.RascunhoJson); } catch (System.Text.Json.JsonException) { }
        return new PropostaPortal(emp, forn, c.Numero, c.Nome, c.EnderecoEntrega, c.Frete, sit.PodeEditar && conv.Status != StatusConvite.Declinou, conv, itens, fat, destino)
            { Locais = porItem, Material = c.Tipo == TipoRequisicao.Material, MarcasPedidas = marcas, PrazoResposta = c.PrazoResposta, Rascunho = rasc, Versoes = versoes, Situacao = sit };
    }

    public async Task EnviarPropostaAsync(Guid token, IReadOnlyList<ItemPropostaPortal> itens, decimal? frete, string? condicao, DateOnly? validade,
        DateOnly? prazoEntrega, CancellationToken ct = default, string? enviadoPor = null)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c)) throw new RegraDeNegocioException("Esta cotação já foi encerrada. Fale com o setor de Suprimentos.");
        if (c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException($"O prazo de resposta terminou em {pz.ToLocalTime():dd/MM/yyyy HH:mm}.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        if (conv.Status == StatusConvite.Declinou) throw new RegraDeNegocioException("Você declinou desta cotação. Fale com Suprimentos para reabrir o convite.");
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
        var reqIds = meus.Values.Where(i => i.RequisicaoItemId != null).Select(i => i.RequisicaoItemId!.Value).ToList();
        var pedidas = await db.Set<RequisicaoItem>().IgnoreQueryFilters().AsNoTracking().Where(r => reqIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => (r.Marca, r.MesmaMarca), ct);
        foreach (var it in itens.Where(i => meus.ContainsKey(i.ItemId)))
        {
            var ci = meus[it.ItemId];
            var pr = ci.RequisicaoItemId is { } rq && pedidas.TryGetValue(rq, out var x) ? x : (null, false);
            var teste = new CotacaoItem { Descricao = ci.Descricao, ValorOfertado = it.ValorUnitario, Marca = it.Marca };
            if (teste.ErroMarca(c.Tipo == TipoRequisicao.Material, pr.Item1, pr.Item2) is { } em) erros.Add(em);
        }
        if (frete is < 0) erros.Add("Frete negativo.");
        if (validade is { } v && v < hoje) erros.Add("Validade da proposta no passado.");
        ValidacaoException.Se(erros);

        foreach (var it in itens)
        {
            var ci = meus[it.ItemId];
            ci.ValorOfertado = Math.Round(it.ValorUnitario, 4);
            ci.Entrega = it.Entrega ?? prazoEntrega;
            ci.Marca = string.IsNullOrWhiteSpace(it.Marca) ? ci.Marca : it.Marca.Trim();
        }
        var marcas = await MarcasServico.NormalizarAsync(db, meus.Values.Select(i => i.Marca), ct);
        foreach (var ci in meus.Values) ci.Marca = MarcasServico.Oficial(marcas, ci.Marca);
        conv.ValorFrete = c.Frete == TipoFrete.NaoSeAplica ? null : frete; conv.CondicaoPagamento = string.IsNullOrWhiteSpace(condicao) ? null : condicao.Trim();
        conv.ValidadeProposta = validade; conv.PrazoEntregaOfertado = prazoEntrega;
        conv.Status = StatusConvite.Respondido; conv.DataResposta = hoje;
        conv.RascunhoEm = null; conv.RascunhoJson = null;
        // Cada envio guarda uma versão (o comprador vê a última e o histórico).
        var nVersao = await db.PropostasVersao.CountAsync(v => v.CotacaoFornecedorId == conv.Id, ct) + 1;
        db.PropostasVersao.Add(new PropostaVersao
        {
            CotacaoFornecedorId = conv.Id, Versao = nVersao, EnviadaEm = relogio.GetUtcNow(), EnviadaPor = enviadoPor,
            ValorItens = meus.Values.Sum(i => Math.Round(i.ValorOfertado * i.Quantidade, 2)), ValorFrete = conv.ValorFrete, PrazoEntrega = prazoEntrega,
            CondicaoPagamento = conv.CondicaoPagamento, Validade = validade,
            ItensJson = System.Text.Json.JsonSerializer.Serialize(meus.Values.OrderBy(i => i.Descricao).Select(i => new { i.Descricao, i.Unidade, i.Quantidade, i.ValorOfertado, i.Marca, i.Entrega }))
        });
        if (c.Status is StatusCotacao.Aberto or StatusCotacao.AguardandoPropostas) c.Status = StatusCotacao.EmAnalise;
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }
    // ---------------- Proposta: rascunho e declínio ----------------

    public async Task SalvarRascunhoAsync(Guid token, RascunhoProposta r, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c) || c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException("Esta cotação não está mais recebendo propostas.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        conv.RascunhoJson = System.Text.Json.JsonSerializer.Serialize(r);
        conv.RascunhoEm = relogio.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Fornecedor informa que não vai cotar (com motivo) ou volta atrás enquanto a cotação está aberta.</summary>
    public async Task DeclinarAsync(Guid token, bool declinar, string? motivo, CancellationToken ct = default)
    {
        if (await LocalizarConviteAsync(token, ct) is not { } loc) throw new RegraDeNegocioException("Link inválido.");
        await using var db = sistema.Criar(loc.EmpresaId, "PortalCotacao");
        var c = await db.Cotacoes.IgnoreQueryFilters().Include(x => x.Fornecedores).Include(x => x.Itens).FirstAsync(x => x.Id == loc.CotacaoId, ct);
        if (!CotacaoAberta(c) || c.PrazoResposta is { } pz && pz <= relogio.GetUtcNow()) throw new RegraDeNegocioException("Esta cotação já foi encerrada.");
        var conv = c.Fornecedores.First(f => f.LinkResposta == token);
        if (declinar)
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new RegraDeNegocioException("Informe o motivo (ex.: sem estoque, sem equipe disponível).");
            conv.Status = StatusConvite.Declinou; conv.MotivoDeclinio = motivo.Trim().Length > 300 ? motivo.Trim()[..300] : motivo.Trim();
        }
        else
        {
            var temPreco = c.Itens.Any(i => i.FornecedorId == conv.FornecedorId && i.ValorOfertado > 0);
            conv.Status = temPreco ? StatusConvite.Respondido : StatusConvite.Convidado; conv.MotivoDeclinio = null;
        }
        c.Recalcular(await ParametrosServico.CarregarAsync(db, ct));
        await db.SaveChangesAsync(ct);
    }

    // ---------------- Início, pedidos e versões de documento ----------------

    public async Task<InicioPortal> InicioAsync(string token, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var agora = relogio.GetUtcNow();
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var pend = new List<PendenciaPortal>();
        foreach (var d in s.Documentos.Where(d => !d.PorEntrega))
        {
            if (d.Situacao == StatusDocumento.Reprovado) pend.Add(new(1, "Recusado", "erro", d.Nome, d.Detalhe ?? "", "empresa", d.TipoId));
            else if (d.Situacao == StatusDocumento.Vencido) pend.Add(new(1, "Vencido", "erro", d.Nome, $"Venceu em {d.Validade:dd/MM/yyyy}", "empresa", d.TipoId));
            else if (d.Situacao == StatusDocumento.PendenteEnvio && d.Obrigatorio) pend.Add(new(2, "Pendente", "aviso", d.Nome, "Documento obrigatório ainda não enviado", "empresa", d.TipoId));
            else if (d.Validade is { } v && v >= hoje && v <= hoje.AddDays(30) && d.Situacao != StatusDocumento.EmValidacao)
                pend.Add(new(v <= hoje.AddDays(7) ? 2 : 3, $"Vence em {v.DayNumber - hoje.DayNumber} dias", "aviso", d.Nome, $"Validade {v:dd/MM/yyyy}", "empresa", d.TipoId));
        }
        var abertos = SincronizacaoCadastros.CotacoesAbertas;
        var cots = await (from cf in db.Set<CotacaoFornecedor>().AsNoTracking()
                          join c in db.Cotacoes.AsNoTracking() on cf.CotacaoId equals c.Id
                          where cf.FornecedorId == s.Fornecedor.Id && abertos.Contains(c.Status) && cf.Status != StatusConvite.Declinou && (c.PrazoResposta == null || c.PrazoResposta > agora)
                          select new { c.Id, c.Numero, c.Nome, c.PrazoResposta, cf.Status, cf.RascunhoEm }).ToListAsync(ct);
        foreach (var c in cots.Where(c => c.Status == StatusConvite.Convidado))
        {
            var horas = c.PrazoResposta is { } pr ? (pr - agora).TotalHours : (double?)null;
            pend.Add(new(horas is < 48 ? 1 : 3, horas is { } h ? (h < 24 ? $"Fecha em {Math.Max(1, (int)h)} h" : $"Fecha {c.PrazoResposta!.Value.ToLocalTime():dd/MM HH'h'}") : "Aberta",
                horas is < 48 ? "aviso" : "info", $"Cotação {c.Numero} · {c.Nome}", c.RascunhoEm is null ? "Proposta não enviada" : "Rascunho salvo, ainda não enviado", "cot", c.Id));
        }
        var cols = s.Colaboradores.Where(c => c.Situacao != SituacaoColaborador.DesligadoDaObra.ToString()).ToList();
        foreach (var c in cols.Where(c => c.Situacao is nameof(SituacaoColaborador.Bloqueado) or nameof(SituacaoColaborador.Pendente) or nameof(SituacaoColaborador.AptoComRestricao)))
            pend.Add(new(c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? 1 : 3, c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? "Funcionário bloqueado" : "Funcionário",
                c.Situacao == nameof(SituacaoColaborador.Bloqueado) ? "erro" : "aviso", c.Nome, c.Pendencias ?? "Documentos pendentes", "func", c.Id));
        var comps = await CompetenciasAsync(token, ct);
        foreach (var c in comps.Where(c => c.Situacao is SituacaoCompetencia.Aberta or SituacaoCompetencia.ComPendencia))
            pend.Add(new(c.Atrasada ? 1 : 2, c.Atrasada ? "Atrasada" : c.Situacao == SituacaoCompetencia.ComPendencia ? "Com pendência" : "Competência aberta", c.Atrasada ? "erro" : "info",
                $"Documentação mensal {c.Mes:MM/yyyy} · contrato {c.Contrato} {c.Obra}", $"{c.Entregues} de {c.Total} itens · limite {c.Limite:dd/MM}", "med", c.Id));
        var bloqueado = TravasQualidade.DocumentosBloqueantes(s.Fornecedor, hoje).ToList();
        var st = s.Fornecedor.CalcularStatus(hoje);
        var (sit, tom, motivo) = bloqueado.Count > 0 || st is StatusQualificacao.Desqualificado
            ? ("Bloqueado", "erro", bloqueado.Count > 0 ? string.Join(" ", bloqueado) + " Pedidos e medições ficam bloqueados até regularizar." : "Fornecedor desqualificado. Fale com Suprimentos.")
            : pend.Count > 0 ? ("Com pendência", "aviso", pend.OrderBy(p => p.Urgencia).First() is var p1 ? $"{p1.Titulo}: {p1.Detalhe}." : "")
            : ("Apto", "ok", "Tudo em dia.");
        var comp = comps.Where(c => c.Situacao != SituacaoCompetencia.Conferida).OrderByDescending(c => c.Mes).FirstOrDefault();
        var emails = s.Fornecedor.Contatos.Where(c => c.RecebeAvisos && !string.IsNullOrWhiteSpace(c.Email)).Select(c => c.Email!).ToList();
        if (emails.Count == 0 && !string.IsNullOrWhiteSpace(s.Fornecedor.Emails)) emails = s.Fornecedor.Emails.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList();
        var docsAtivos = s.Documentos.Where(d => !d.PorEntrega).ToList();
        var urgente = cots.Where(c => c.PrazoResposta != null).OrderBy(c => c.PrazoResposta).FirstOrDefault();
        return new InicioPortal(sit, tom, motivo, cots.Count, urgente is null ? null : $"cotação {urgente.Numero} fecha {urgente.PrazoResposta!.Value.ToLocalTime():dd/MM HH'h'}",
            docsAtivos.Count(d => d.Situacao is StatusDocumento.Valido or StatusDocumento.AVencer), docsAtivos.Count, docsAtivos.Count(d => d.Situacao == StatusDocumento.AVencer),
            docsAtivos.Count(d => d.Situacao == StatusDocumento.Reprovado), cols.Count, cols.Count(c => c.Situacao is nameof(SituacaoColaborador.Bloqueado) or nameof(SituacaoColaborador.Pendente)),
            cols.Count(c => c.AguardandoValidacao), comp is null ? null : $"{comp.Mes:MM/yyyy}", comp?.Entregues ?? 0, comp?.Total ?? 0,
            pend.OrderBy(p => p.Urgencia).ThenBy(p => p.Titulo).ToList(), emails);
    }

    public async Task<IReadOnlyList<PedidoPortal>> PedidosAsync(string token, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var visiveis = new[] { StatusPedido.Aprovado, StatusPedido.EnviadoFornecedor, StatusPedido.EntregueParcial, StatusPedido.Entregue };
        var peds = await db.Pedidos.AsNoTracking().Include(p => p.Itens).Where(p => p.FornecedorId == s.Fornecedor.Id && visiveis.Contains(p.Status))
            .OrderByDescending(p => p.Numero).Take(100).ToListAsync(ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return peds.Select(p => new PedidoPortal(p.Id, p.Numero, obras.GetValueOrDefault(p.ObraId, ""), DateOnly.FromDateTime(p.CriadoEm.ToLocalTime().Date), p.Entrega, p.Status, p.Valor,
            p.Itens.Select(i => (i.Descricao, i.Unidade ?? "", i.Quantidade, i.QuantidadeRecebidaAceita)).ToList())).ToList();
    }

    public async Task<IReadOnlyList<VersaoDocumentoPortal>> VersoesDocumentoAsync(string token, Guid tipoId, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var l = await db.FornecedorDocumentos.AsNoTracking().Include(d => d.TipoDocumento).Where(d => d.FornecedorId == s.Fornecedor.Id && d.TipoDocumentoExigidoId == tipoId)
            .OrderByDescending(d => d.CriadoEm).Take(30).ToListAsync(ct);
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        var vigente = l.Where(d => d.Status is not (StatusDocumento.EmValidacao or StatusDocumento.Reprovado) && d.ArquivoBlob is not null).Select(d => (Guid?)d.Id).FirstOrDefault();
        return l.Select(d => new VersaoDocumentoPortal(d.Id, d.CriadoEm, d.ResponsavelEnvio ?? d.CriadoPor, d.DataValidade, StatusDocumentoCalculo.Calcular(d, hoje), d.MotivoRecusa, d.ArquivoNomeOriginal, d.Id == vigente)).ToList();
    }

    // ---------------- Funcionários cadastrados pelo fornecedor ----------------

    /// <summary>
    /// Cadastro do funcionário pelo fornecedor: entra "Aguardando validação" e só vai para a obra depois que a construtora valida.
    /// CPF protegido (só o hash e a máscara ficam gravados). Desligamento tira da lista ativa e da cobrança de documentos.
    /// </summary>
    public async Task<Guid> SalvarColaboradorAsync(string token, ColaboradorForm f, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(f.Nome) || f.Nome.Trim().Length > 150) e.Add("Informe o nome (até 150 caracteres).");
        if (s.Funcoes.All(x => x.Id != f.FuncaoId)) e.Add("Escolha a função.");
        if (f.Admissao == default) e.Add("Informe a data de admissão.");
        if (f.Desligamento is { } dsl && dsl < f.Admissao) e.Add("Desligamento antes da admissão.");
        var contrato = f.ContratoId is { } kid ? s.Contratos.FirstOrDefault(k => k.Id == kid) : s.Contratos.Count == 1 ? s.Contratos[0] : null;
        if (contrato is null) e.Add("Escolha a obra/contrato em que o funcionário vai trabalhar.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        Domain.Sst.Colaborador? c = null;
        if (f.Id is { } id)
        {
            c = await db.Colaboradores.FirstOrDefaultAsync(x => x.Id == id && x.FornecedorId == s.Fornecedor.Id, ct);
            if (c is null) e.Add("Funcionário não pertence a esta empresa.");
        }
        string? hash = null, mascara = null;
        if (!string.IsNullOrWhiteSpace(f.Cpf))
        {
            var d = Conversor.SomenteDigitos(f.Cpf);
            if (!Conversor.CpfValido(d)) e.Add("CPF inválido.");
            else { hash = cpf.Hash(d); mascara = ProtecaoCpf.Mascarar(d); }
        }
        else if (c is null) e.Add("Informe o CPF.");
        if (hash is not null && contrato is not null && await db.Colaboradores.AnyAsync(x => x.CpfHash == hash && x.ObraId == contrato.ObraId && x.Id != (f.Id ?? Guid.Empty), ct))
            e.Add("Já existe funcionário com este CPF nesta obra.");
        ValidacaoException.Se(e);
        if (c is null)
        {
            c = new Domain.Sst.Colaborador
            {
                EmpresaId = s.EmpresaId, Vinculo = VinculoColaborador.Terceiro, FornecedorId = s.Fornecedor.Id, CadastradoPeloFornecedor = true,
                AguardandoValidacao = true, Situacao = SituacaoColaborador.AguardandoValidacao, Pendencias = "Aguardando validação da construtora"
            };
            db.Colaboradores.Add(c);
        }
        var mudouObra = c.ObraId != contrato!.ObraId || c.ContratoId != contrato.Id;
        c.Nome = f.Nome.Trim(); c.FuncaoObraId = f.FuncaoId; c.DataInicio = f.Admissao; c.DataSaida = f.Desligamento;
        c.ObraId = contrato.ObraId; c.ContratoId = contrato.Id;
        if (hash is not null) { mudouObra |= c.CpfHash != hash; c.CpfHash = hash; c.CpfMascarado = mascara!; }
        if (mudouObra && c.CriadoEm != default && !c.AguardandoValidacao)
        {
            c.AguardandoValidacao = true; c.Situacao = SituacaoColaborador.AguardandoValidacao; c.Pendencias = "Dados alterados pelo fornecedor: aguardando nova validação";
        }
        if (c.DataSaida is { } saida && saida <= DateOnly.FromDateTime(relogio.GetLocalNow().Date)) c.Situacao = SituacaoColaborador.DesligadoDaObra;
        await db.SaveChangesAsync(ct);
        return c.Id;
    }

    // ---------------- Documentação mensal (competências) ----------------

    public async Task<IReadOnlyList<CompetenciaPortal>> CompetenciasAsync(string token, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().Date);
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        if (s.Contratos.Count > 0) { await CompetenciasServico.GarantirAtivosAsync(db, hoje, s.Fornecedor.Id, ct); await db.SaveChangesAsync(ct); }
        var p = await ParametrosServico.CarregarAsync(db, ct);
        var dia = p.Int(Domain.Estrutura.Parametros.DiaLimiteCompetencia);
        var l = await db.CompetenciasDocumentais.AsNoTracking().Include(c => c.Itens).Where(c => c.FornecedorId == s.Fornecedor.Id).OrderByDescending(c => c.Mes).Take(60).ToListAsync(ct);
        var ks = s.Contratos.ToDictionary(k => k.Id);
        var nums = await db.Contratos.AsNoTracking().Where(k => k.FornecedorId == s.Fornecedor.Id).ToDictionaryAsync(k => k.Id, k => (k.Numero, k.ObraId), ct);
        var obras = await db.Obras.AsNoTracking().ToDictionaryAsync(o => o.Id, o => o.Nome, ct);
        return l.Select(c => new CompetenciaPortal(c.Id, c.ContratoId, nums.GetValueOrDefault(c.ContratoId).Numero ?? "", obras.GetValueOrDefault(c.ObraId, ""), c.Mes, c.Situacao,
            c.Atrasada(hoje, dia), c.Entregues, c.Itens.Count, c.Limite(dia))).ToList();
    }

    private async Task<(SessaoPortal S, ISigeoDb Db, CompetenciaDocumental C)> CompetenciaDoFornecedorAsync(string token, Guid competenciaId, CancellationToken ct)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        var db = sistema.Criar(s.EmpresaId, "Portal");
        var c = await db.CompetenciasDocumentais.Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == competenciaId && x.FornecedorId == s.Fornecedor.Id, ct);
        if (c is null) { await db.DisposeAsync(); throw new RegraDeNegocioException("Competência não encontrada para esta empresa."); }
        return (s, db, c);
    }

    public async Task<CompetenciaDetalhe> CompetenciaAsync(string token, Guid competenciaId, CancellationToken ct = default)
    {
        var (_, db, c) = await CompetenciaDoFornecedorAsync(token, competenciaId, ct);
        await using var _d = db;
        return await CompetenciasServico.DetalheAsync(db, c, DateOnly.FromDateTime(relogio.GetLocalNow().Date), ct);
    }

    public async Task EnviarItemCompetenciaAsync(string token, Guid competenciaId, Guid itemId, string responsavel, Stream arquivo, string nomeArquivo, string? contentType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(responsavel) || responsavel.Length > 150) throw new RegraDeNegocioException("Informe o responsável pelo envio (até 150 caracteres).");
        var (s, db, c) = await CompetenciaDoFornecedorAsync(token, competenciaId, ct);
        await using var _d = db;
        if (c.Situacao == SituacaoCompetencia.Conferida) throw new RegraDeNegocioException("Competência já conferida (fechada). Fale com a construtora para reabrir.");
        var i = c.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new RegraDeNegocioException("Item não pertence a esta competência.");
        arquivo = await ArquivoPortal.ConferirAsync(arquivo, nomeArquivo, ct);
        var salvo = await armazenamento.SalvarAsync(arquivo, nomeArquivo, i.ColaboradorId is null ? "fornecedores" : "sst", contentType, ct);
        var agora = relogio.GetUtcNow();
        i.Enviado(salvo.Blob, Path.GetFileName(nomeArquivo), responsavel.Trim(), agora);
        if (i.ColaboradorId is null)
        {
            // Documento da empresa: também vira documento do fornecedor da competência (cadastro e relatórios).
            var bloqueia = await db.TiposDocumento.Where(t => t.Id == i.TipoDocumentoExigidoId).Select(t => t.BloqueiaQuandoVencido).FirstAsync(ct);
            foreach (var antigo in await db.FornecedorDocumentos.Where(x => x.FornecedorId == s.Fornecedor.Id && x.TipoDocumentoExigidoId == i.TipoDocumentoExigidoId && x.Competencia == c.Mes).ToListAsync(ct))
                db.Remove(antigo);
            var doc = new FornecedorDocumento
            {
                EmpresaId = s.EmpresaId, FornecedorId = s.Fornecedor.Id, TipoDocumentoExigidoId = i.TipoDocumentoExigidoId, Competencia = c.Mes, ArquivoBlob = salvo.Blob,
                ArquivoNomeOriginal = Path.GetFileName(nomeArquivo), ArquivoHashSha256 = salvo.Hash, Status = StatusDocumento.EmValidacao, EnviadoPeloPortal = true,
                ResponsavelEnvio = responsavel.Trim(), Bloqueia = bloqueia
            };
            db.FornecedorDocumentos.Add(doc);
            i.FornecedorDocumentoId = doc.Id;
        }
        if (c.Situacao is SituacaoCompetencia.ComPendencia && c.Itens.All(x => x.Situacao is not (SituacaoItemCompetencia.Pendente or SituacaoItemCompetencia.Recusado))) c.Situacao = SituacaoCompetencia.Enviada;
        await db.SaveChangesAsync(ct);
    }

    public async Task SolicitarAdiamentoAsync(string token, Guid competenciaId, Guid itemId, string? justificativa, DateOnly? ate, CancellationToken ct = default)
    {
        var (_, db, c) = await CompetenciaDoFornecedorAsync(token, competenciaId, ct);
        await using var _d = db;
        var i = c.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new RegraDeNegocioException("Item não pertence a esta competência.");
        i.SolicitarAdiamento(justificativa, ate, DateOnly.FromDateTime(relogio.GetLocalNow().Date));
        await db.SaveChangesAsync(ct);
    }

    public async Task MarcarCompetenciaCompletaAsync(string token, Guid competenciaId, string responsavel, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(responsavel)) throw new RegraDeNegocioException("Informe o responsável.");
        var (_, db, c) = await CompetenciaDoFornecedorAsync(token, competenciaId, ct);
        await using var _d = db;
        c.Enviar(responsavel.Trim(), relogio.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Contato que recebe os avisos (vencimento, recusa, competência): marcado pelo próprio fornecedor.</summary>
    public async Task DefinirAvisosAsync(string token, IReadOnlyCollection<Guid> contatos, CancellationToken ct = default)
    {
        var s = await AbrirAsync(token, null, ct) ?? throw new RegraDeNegocioException("Sessão expirada.");
        await using var db = sistema.Criar(s.EmpresaId, "Portal");
        var l = await db.FornecedorContatos.Where(c => c.FornecedorId == s.Fornecedor.Id).ToListAsync(ct);
        if (contatos.Any(id => l.All(c => c.Id != id))) throw new RegraDeNegocioException("Contato inválido.");
        if (contatos.Count > 0 && l.Where(c => contatos.Contains(c.Id)).All(c => string.IsNullOrWhiteSpace(c.Email))) throw new RegraDeNegocioException("Escolha ao menos um contato com e-mail.");
        foreach (var c in l) c.RecebeAvisos = contatos.Contains(c.Id);
        await db.SaveChangesAsync(ct);
    }

}

/// <summary>Conferência do arquivo enviado pelo portal: extensão permitida e conteúdo compatível (assinatura do arquivo), até 20 MB.</summary>
public static class ArquivoPortal
{
    public const long TamanhoMaximo = 20 * 1024 * 1024;

    /// <summary>Confere e devolve um stream posicionável (copia para memória quando o original não permite voltar ao início).</summary>
    public static async Task<Stream> ConferirAsync(Stream arquivo, string nomeArquivo, CancellationToken ct)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png")) throw new RegraDeNegocioException("Envie PDF, JPG ou PNG.");
        if (!arquivo.CanSeek)
        {
            var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms, ct);
            ms.Position = 0;
            arquivo = ms;
        }
        if (arquivo.Length > TamanhoMaximo) throw new RegraDeNegocioException("Arquivo maior que 20 MB.");
        var cab = new byte[8];
        var lidos = await arquivo.ReadAtLeastAsync(cab, cab.Length, throwOnEndOfStream: false, ct);
        arquivo.Position = 0;
        var ok = ext switch
        {
            ".pdf" => lidos >= 4 && cab[0] == 0x25 && cab[1] == 0x50 && cab[2] == 0x44 && cab[3] == 0x46,             // %PDF
            ".png" => lidos >= 8 && cab[0] == 0x89 && cab[1] == 0x50 && cab[2] == 0x4E && cab[3] == 0x47,             // .PNG
            _ => lidos >= 3 && cab[0] == 0xFF && cab[1] == 0xD8 && cab[2] == 0xFF                                     // JPEG
        };
        if (!ok) throw new RegraDeNegocioException("O conteúdo do arquivo não corresponde à extensão (envie um PDF, JPG ou PNG válido).");
        return arquivo;
    }
}
