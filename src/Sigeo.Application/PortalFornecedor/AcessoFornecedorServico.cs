using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>
/// Área do Fornecedor: entrada por CNPJ/CPF + código de uso único enviado a um contato do cadastro (e-mail ou WhatsApp).
/// Roda com identidade de sistema porque o fornecedor não é usuário interno; toda consulta confere que o CNPJ/contato
/// verificado tem direito ao fornecedor pedido.
/// </summary>
public sealed class AcessoFornecedorServico(IFabricaSistema sistema, IEnvioMensagens envio, TimeProvider relogio)
{
    public const int MaxCodigosPorDocumentoHora = 3;
    public const int MaxCodigosPorIpHora = 10;
    public const int HorasSessaoPortal = 8;

    public static string? Documento(string? bruto)
    {
        var d = Conversor.SomenteDigitos(bruto);
        return Conversor.CnpjValido(d) || Conversor.CpfValido(d) ? d : null;
    }

    private static string Formatado(string d) => d.Length == 14 ? Conversor.FormatarCnpj(d) : Convert.ToUInt64(d).ToString(@"000\.000\.000\-00");

    /// <summary>Fornecedores (em todas as construtoras) com o documento — ativos e não excluídos.</summary>
    private static IQueryable<Fornecedor> PorDocumento(ISigeoDb db, string doc)
    {
        var f = Formatado(doc);
        return db.Fornecedores.IgnoreQueryFilters().Where(x => !x.Excluido && x.Ativo && (x.CpfCnpj == f || x.CpfCnpj == doc));
    }

    /// <summary>Contatos com e-mail ou celular do fornecedor deste CNPJ/CPF, para a pessoa escolher quem recebe o código.</summary>
    public async Task<IReadOnlyList<ContatoAcesso>> ContatosAsync(string? documento, CancellationToken ct = default)
    {
        var doc = Documento(documento) ?? throw new RegraDeNegocioException("Informe um CNPJ ou CPF válido.");
        await using var db = sistema.Criar(Guid.Empty, "AreaFornecedor");
        var ids = await PorDocumento(db, doc).Select(f => f.Id).ToListAsync(ct);
        var contatos = await db.FornecedorContatos.AsNoTracking().Where(c => ids.Contains(c.FornecedorId)).OrderByDescending(c => c.Principal).ThenBy(c => c.Nome).ToListAsync(ct);
        // Mesmo contato cadastrado em mais de uma construtora aparece uma vez só.
        return contatos.Select(c => (c, Email: Destinos.Email(c.Email), Cel: Destinos.Celular(c.Celular) ?? Destinos.Celular(c.Telefone)))
            .Where(x => x.Email is not null || x.Cel is not null)
            .GroupBy(x => (x.Email, x.Cel)).Select(g => g.First())
            .Select(x => new ContatoAcesso(x.c.Id, Destinos.MascararNome(x.c.Nome), x.Email is null ? null : Destinos.MascararEmail(x.Email),
                                           x.Cel is null ? null : Destinos.MascararCelular(x.Cel)))
            .ToList();
    }

    /// <summary>Gera e envia o código. Devolve o destino mascarado para a tela.</summary>
    public async Task<string> EnviarCodigoAsync(string? documento, Guid contatoId, CanalCodigo canal, string? ip, CancellationToken ct = default)
    {
        var doc = Documento(documento) ?? throw new RegraDeNegocioException("Informe um CNPJ ou CPF válido.");
        var agora = relogio.GetUtcNow();
        await using var db = sistema.Criar(Guid.Empty, "AreaFornecedor");
        var contato = await (from c in db.FornecedorContatos.AsNoTracking()
                             join f in PorDocumento(db, doc) on c.FornecedorId equals f.Id
                             where c.Id == contatoId
                             select new { c.Email, c.Celular, c.Telefone, f.Id, f.EmpresaId }).FirstOrDefaultAsync(ct)
                      ?? throw new RegraDeNegocioException("Escolha um dos contatos da lista.");
        var destino = canal == CanalCodigo.Email ? Destinos.Email(contato.Email) : Destinos.Celular(contato.Celular) ?? Destinos.Celular(contato.Telefone);
        if (destino is null) throw new RegraDeNegocioException(canal == CanalCodigo.Email ? "Este contato não tem e-mail cadastrado." : "Este contato não tem celular cadastrado.");
        if (canal == CanalCodigo.WhatsApp && !envio.WhatsAppDisponivel) throw new RegraDeNegocioException("Envio por WhatsApp ainda não está disponível. Use o e-mail.");
        if (canal == CanalCodigo.Email && !envio.EmailDisponivel) throw new RegraDeNegocioException("Envio por e-mail ainda não está disponível. Fale com Suprimentos.");

        // Limites contra abuso (custo de envio e tentativa de adivinhar código).
        var umaHora = agora.AddHours(-1);
        if (await db.CodigosAcessoFornecedor.CountAsync(x => x.Documento == doc && x.CriadoEm > umaHora, ct) >= MaxCodigosPorDocumentoHora)
            throw new RegraDeNegocioException("Muitos códigos pedidos para este CNPJ. Aguarde uma hora ou use o último código recebido.");
        if (ip is not null && await db.CodigosAcessoFornecedor.CountAsync(x => x.Ip == ip && x.CriadoEm > umaHora, ct) >= MaxCodigosPorIpHora)
            throw new RegraDeNegocioException("Muitos códigos pedidos a partir desta rede. Aguarde uma hora.");

        var codigo = CodigoAcessoFornecedor.NovoCodigo();
        var reg = new CodigoAcessoFornecedor
        {
            Documento = doc, EmpresaId = contato.EmpresaId, FornecedorId = contato.Id, ContatoId = contatoId, Canal = canal, Destino = destino,
            CriadoEm = agora, ExpiraEm = agora.AddMinutes(CodigoAcessoFornecedor.ValidadeMinutos), Ip = ip
        };
        reg.CodigoHash = CodigoAcessoFornecedor.Hash(reg.Id, codigo);
        // Um código novo invalida os anteriores ainda vigentes deste documento (só o último vale).
        foreach (var antigo in await db.CodigosAcessoFornecedor.Where(x => x.Documento == doc && x.UsadoEm == null && x.ExpiraEm > agora).ToListAsync(ct))
            antigo.ExpiraEm = agora;
        db.CodigosAcessoFornecedor.Add(reg);
        await db.SaveChangesAsync(ct);

        try
        {
            if (canal == CanalCodigo.Email)
                await envio.EnviarEmailAsync(destino, $"Código de acesso: {codigo}",
                    $"Seu código de acesso à Área do Fornecedor é {codigo}. Ele vale por {CodigoAcessoFornecedor.ValidadeMinutos} minutos e só pode ser usado uma vez. Se você não pediu, ignore esta mensagem.",
                    $"<p>Seu código de acesso à <b>Área do Fornecedor</b> é:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:4px\">{codigo}</p>" +
                    $"<p>Ele vale por {CodigoAcessoFornecedor.ValidadeMinutos} minutos e só pode ser usado uma vez.</p><p style=\"color:#666\">Se você não pediu este código, ignore esta mensagem.</p>", ct);
            else
                await envio.EnviarCodigoWhatsAppAsync(destino, codigo, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException and not RegraDeNegocioException)
        {
            reg.ExpiraEm = agora; // não deixa vigente um código que não chegou
            await db.SaveChangesAsync(CancellationToken.None);
            throw new RegraDeNegocioException("Não foi possível enviar o código agora. Tente de novo em alguns minutos ou escolha outro contato.");
        }
        return canal == CanalCodigo.Email ? Destinos.MascararEmail(destino) : Destinos.MascararCelular(destino);
    }

    /// <summary>Confere o código do documento (o último enviado). Errou: conta tentativa. Certo: devolve a identidade.</summary>
    public async Task<IdentidadeFornecedor> EntrarAsync(string? documento, string? codigo, CancellationToken ct = default)
    {
        var doc = Documento(documento) ?? throw new RegraDeNegocioException("Informe um CNPJ ou CPF válido.");
        var cod = Conversor.SomenteDigitos(codigo);
        var agora = relogio.GetUtcNow();
        await using var db = sistema.Criar(Guid.Empty, "AreaFornecedor");
        var reg = await db.CodigosAcessoFornecedor.Where(x => x.Documento == doc && x.UsadoEm == null && x.ExpiraEm > agora)
                         .OrderByDescending(x => x.CriadoEm).FirstOrDefaultAsync(ct);
        if (reg is null || cod.Length != 6) throw new RegraDeNegocioException("Código inválido ou vencido. Peça um novo código.");
        var ok = reg.Conferir(cod, agora);
        await db.SaveChangesAsync(ct);
        if (!ok)
            throw new RegraDeNegocioException(reg.Tentativas >= CodigoAcessoFornecedor.MaxTentativas
                ? "Código bloqueado por excesso de tentativas. Peça um novo código."
                : $"Código incorreto. Restam {CodigoAcessoFornecedor.MaxTentativas - reg.Tentativas} tentativa(s).");
        return new IdentidadeFornecedor(doc, reg.Destino);
    }

    /// <summary>
    /// CNPJs que a pessoa pode representar: o do documento digitado e os outros fornecedores em que o mesmo
    /// e-mail/celular verificado está cadastrado como contato (ex.: matriz e filial, empresas do mesmo grupo).
    /// </summary>
    public async Task<IReadOnlyList<AcessoCnpj>> AcessosAsync(IdentidadeFornecedor id, CancellationToken ct = default)
    {
        await using var db = sistema.Criar(Guid.Empty, "AreaFornecedor");
        var porDoc = await PorDocumento(db, id.Documento).Select(f => f.Id).ToListAsync(ct);
        List<Guid> porContato;
        if (id.DestinoVerificado.Contains('@'))
            porContato = await db.FornecedorContatos.AsNoTracking().Where(c => c.Email != null && c.Email.Trim().ToLower() == id.DestinoVerificado)
                                 .Select(c => c.FornecedorId).ToListAsync(ct);
        else
        {
            var fim = id.DestinoVerificado[^8..]; // filtra no banco pelos 8 últimos dígitos e confere o número completo em memória
            porContato = (await db.FornecedorContatos.AsNoTracking().Where(c => (c.Celular != null && c.Celular.Replace("-", "").Replace(" ", "").EndsWith(fim))
                                                                            || (c.Telefone != null && c.Telefone.Replace("-", "").Replace(" ", "").EndsWith(fim)))
                                 .Select(c => new { c.FornecedorId, c.Celular, c.Telefone }).ToListAsync(ct))
                         .Where(c => Destinos.Celular(c.Celular) == id.DestinoVerificado || Destinos.Celular(c.Telefone) == id.DestinoVerificado)
                         .Select(c => c.FornecedorId).ToList();
        }
        var ids = porDoc.Concat(porContato).Distinct().ToList();
        var fs = await db.Fornecedores.IgnoreQueryFilters().AsNoTracking().Where(f => ids.Contains(f.Id) && !f.Excluido && f.Ativo)
                         .Select(f => new { f.Id, f.EmpresaId, f.RazaoSocial, f.CpfCnpj }).ToListAsync(ct);
        var empIds = fs.Select(f => f.EmpresaId).Distinct().ToList();
        var empresas = await db.Empresas.IgnoreQueryFilters().AsNoTracking().Where(e => empIds.Contains(e.Id))
                               .ToDictionaryAsync(e => e.Id, e => e.NomeFantasia != "" ? e.NomeFantasia : e.RazaoSocial, ct);
        var abertos = new[] { StatusCotacao.Aberto, StatusCotacao.AguardandoPropostas, StatusCotacao.EmAnalise, StatusCotacao.ReprovadoVoltaCotacao };
        var fIds = fs.Select(f => f.Id).ToList();
        var contagem = await (from cf in db.Set<CotacaoFornecedor>().AsNoTracking()
                              join c in db.Cotacoes.IgnoreQueryFilters().AsNoTracking() on cf.CotacaoId equals c.Id
                              where fIds.Contains(cf.FornecedorId) && !c.Excluido && abertos.Contains(c.Status) && cf.Status != StatusConvite.Declinou
                              group cf by cf.FornecedorId into g
                              select new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        return fs.Select(f => new AcessoCnpj(f.EmpresaId, empresas.GetValueOrDefault(f.EmpresaId, "Construtora"), f.Id, f.RazaoSocial, f.CpfCnpj, contagem.GetValueOrDefault(f.Id)))
                 .OrderBy(a => a.Construtora).ThenBy(a => a.RazaoSocial).ToList();
    }

    /// <summary>Garante que a identidade pode agir pelo fornecedor (nunca confia no id vindo da tela).</summary>
    public async Task<AcessoCnpj> ExigirAcessoAsync(IdentidadeFornecedor id, Guid fornecedorId, CancellationToken ct = default) =>
        (await AcessosAsync(id, ct)).FirstOrDefault(a => a.FornecedorId == fornecedorId)
        ?? throw new RegraDeNegocioException("Você não tem acesso a este fornecedor.");

    /// <summary>
    /// Histórico de todas as cotações em que o fornecedor foi convidado, com a situação vista por ele (vencedora, parcial,
    /// não vencedora, prazo encerrado…), versões enviadas e o pedido/contrato gerado. Nunca mostra concorrentes.
    /// </summary>
    public async Task<IReadOnlyList<CotacaoDoFornecedor>> CotacoesAsync(IdentidadeFornecedor id, Guid fornecedorId, CancellationToken ct = default)
    {
        var a = await ExigirAcessoAsync(id, fornecedorId, ct);
        await using var db = sistema.Criar(a.EmpresaId, "AreaFornecedor");
        var agora = relogio.GetUtcNow();
        var lista = await (from cf in db.Set<CotacaoFornecedor>().AsNoTracking()
                           join c in db.Cotacoes.AsNoTracking() on cf.CotacaoId equals c.Id
                           join o in db.Obras.AsNoTracking() on c.ObraId equals o.Id into og
                           from o in og.DefaultIfEmpty()
                           where cf.FornecedorId == fornecedorId
                           orderby c.Numero descending
                           select new { c, cf, Obra = o != null ? o.Nome : null }).Take(300).ToListAsync(ct);
        var cotIds = lista.Select(x => x.c.Id).ToList();
        var itens = await db.Set<CotacaoItem>().AsNoTracking().Where(i => cotIds.Contains(i.CotacaoId))
            .Select(i => new { i.CotacaoId, i.FornecedorId, i.Vencedor, i.ValorOfertado, i.ValorNegociado, i.Quantidade }).ToListAsync(ct);
        var convIds = lista.Select(x => x.cf.Id).ToList();
        var versoes = await db.PropostasVersao.AsNoTracking().Where(v => convIds.Contains(v.CotacaoFornecedorId)).GroupBy(v => v.CotacaoFornecedorId)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var pedidos = await db.Pedidos.AsNoTracking().Where(p => p.CotacaoId != null && cotIds.Contains(p.CotacaoId.Value) && p.FornecedorId == fornecedorId && p.Status != StatusPedido.Cancelado)
            .Select(p => new { CotacaoId = p.CotacaoId!.Value, p.Numero }).ToListAsync(ct);
        var contratos = await db.Contratos.AsNoTracking().Where(k => k.CotacaoId != null && cotIds.Contains(k.CotacaoId.Value) && k.FornecedorId == fornecedorId)
            .Select(k => new { CotacaoId = k.CotacaoId!.Value, k.Numero }).ToListAsync(ct);
        return lista.Select(x =>
        {
            var meus = itens.Where(i => i.CotacaoId == x.c.Id && i.FornecedorId == fornecedorId).ToList();
            var porItem = itens.Any(i => i.CotacaoId == x.c.Id && i.Vencedor);
            var decidida = x.c.Status is StatusCotacao.AguardandoAprovacao or StatusCotacao.Aprovado or StatusCotacao.PedidoGerado;
            var venc = !decidida ? 0 : porItem ? meus.Count(i => i.Vencedor) : x.c.FornecedorVencedorId == fornecedorId ? meus.Count : 0;
            x.cf.Versoes = Enumerable.Range(0, versoes.GetValueOrDefault(x.cf.Id)).Select(_ => new PropostaVersao()).ToList();
            var s = SituacaoPropostaFornecedor.Calcular(x.c.Status, x.c.PrazoResposta, x.cf, venc, meus.Count, decidida, agora);
            var res = string.Join(" · ", pedidos.Where(p => p.CotacaoId == x.c.Id).Select(p => $"Pedido {p.Numero}").Concat(contratos.Where(k => k.CotacaoId == x.c.Id).Select(k => $"Contrato {k.Numero}")));
            var enviada = x.cf.Status is StatusConvite.Respondido or StatusConvite.Vencedor;
            decimal? total = enviada ? meus.Sum(i => Math.Round((i.ValorNegociado ?? i.ValorOfertado) * i.Quantidade, 2)) : null;
            return new CotacaoDoFornecedor(x.c.Id, x.c.Numero, x.c.Nome, x.Obra, s.Texto, enviada, s.PodeEditar, x.cf.DataResposta)
            {
                Codigo = s.Situacao, Tom = s.Tom, Prazo = x.c.PrazoResposta, CriadaEm = x.c.CriadoEm, Resultado = res.Length == 0 ? null : res,
                Versoes = versoes.GetValueOrDefault(x.cf.Id), Total = total, Tipo = x.c.Tipo, Itens = meus.Count, MotivoDeclinio = x.cf.MotivoDeclinio
            };
        }).ToList();
    }

    /// <summary>Chave interna do convite (fica no servidor; a tela da proposta usa por dentro, sem expor na URL).</summary>
    public async Task<Guid> ConviteAsync(IdentidadeFornecedor id, Guid fornecedorId, Guid cotacaoId, CancellationToken ct = default)
    {
        var a = await ExigirAcessoAsync(id, fornecedorId, ct);
        await using var db = sistema.Criar(a.EmpresaId, "AreaFornecedor");
        return await db.Set<CotacaoFornecedor>().AsNoTracking().Where(x => x.CotacaoId == cotacaoId && x.FornecedorId == fornecedorId)
                       .Select(x => (Guid?)x.LinkResposta).FirstOrDefaultAsync(ct)
               ?? throw new RegraDeNegocioException("Cotação não encontrada para este fornecedor.");
    }

    /// <summary>
    /// Sessão do portal de documentos/cadastro para o fornecedor escolhido: um link interno de curta duração
    /// (o mesmo mecanismo do link enviado), usado só no servidor.
    /// </summary>
    public async Task<string> SessaoPortalAsync(IdentidadeFornecedor id, Guid fornecedorId, CancellationToken ct = default)
    {
        var a = await ExigirAcessoAsync(id, fornecedorId, ct);
        await using var db = sistema.Criar(a.EmpresaId, "AreaFornecedor");
        var token = Token.Novo();
        db.LinksPortal.Add(new LinkPortalFornecedor
        {
            EmpresaId = a.EmpresaId, FornecedorId = fornecedorId, Codigo = $"ACS-{await Numerador.ProximoAsync(db, "LinkPortal", ct):0000}",
            TokenHash = Token.Hash(token), ValidoAte = relogio.GetUtcNow().AddHours(HorasSessaoPortal), EmailDestino = id.DestinoVerificado,
            Motivo = "Acesso por código", EnviadoPor = "Área do fornecedor", Status = StatusSolicitacaoDoc.Acessada
        });
        await db.SaveChangesAsync(ct);
        return token;
    }
}
