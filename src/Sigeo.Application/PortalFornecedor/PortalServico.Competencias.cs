using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

// Competencias
public sealed partial class PortalServico
{
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
