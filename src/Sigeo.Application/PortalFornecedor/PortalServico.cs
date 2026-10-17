using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>
/// Portal do fornecedor: acesso externo por link com token (sem login), prazo e registro de IP.
/// O documento enviado entra "Em validação" — Suprimentos aprova ou recusa com motivo.
/// </summary>
public sealed partial class PortalServico(IFabricaSistema sistema, IFabricaDb fabrica, IArmazenamento armazenamento, IUsuarioAtual usuario, TimeProvider relogio, ProtecaoCpf cpf)
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
}
