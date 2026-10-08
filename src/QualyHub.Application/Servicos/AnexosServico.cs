using Microsoft.EntityFrameworkCore;
using QualyHub.Domain.Common;

namespace QualyHub.Application.Servicos;

/// <summary>Arquivo vindo da tela, sem depender do tipo do Blazor (o serviço só precisa abrir o conteúdo com um teto de bytes).</summary>
public sealed record ArquivoEnviado(string Nome, long Tamanho, string? ContentType, Func<long, Stream> Abrir);

/// <summary>Anexos e fotos de qualquer registro: grava o arquivo no armazenamento (com SHA-256) e o registro do anexo.</summary>
public sealed class AnexosServico(IFabricaDb fabrica, IArmazenamento armazenamento)
{
    public const long LimiteAnexoBytes = 25 * 1024 * 1024;
    public const long LimiteFotoBytes = 15 * 1024 * 1024;

    /// <summary>
    /// Salva os arquivos e registra os anexos numa única gravação. Arquivos acima do limite são recusados
    /// (os demais seguem) e voltam como mensagens para a tela.
    /// </summary>
    public async Task<IReadOnlyList<string>> AnexarAsync(string entidade, Guid entidadeId, string categoria, string pasta, long limiteBytes,
        IEnumerable<ArquivoEnviado> arquivos, CancellationToken ct = default)
    {
        ValidacaoException.Se(ValidarDestino(entidade, entidadeId));
        var recusados = new List<string>();
        await using var db = fabrica.Criar();
        foreach (var f in arquivos)
        {
            if (ValidarTamanho(f.Nome, f.Tamanho, limiteBytes) is { } erro) { recusados.Add(erro); continue; }
            await using var s = f.Abrir(limiteBytes);
            var salvo = await armazenamento.SalvarAsync(s, f.Nome, pasta, f.ContentType, ct);
            db.Anexos.Add(new Anexo
            {
                Entidade = entidade, EntidadeId = entidadeId, Categoria = string.IsNullOrWhiteSpace(categoria) ? "Geral" : categoria,
                NomeOriginal = f.Nome, ArquivoBlob = salvo.Blob, ArquivoHashSha256 = salvo.Hash, ContentType = f.ContentType, TamanhoBytes = salvo.Tamanho
            });
        }
        await db.SaveChangesAsync(ct);
        return recusados;
    }

    /// <summary>
    /// Exclusão lógica do registro; o arquivo continua no armazenamento porque o anexo pode ser evidência de auditoria
    /// (o log mostra quem excluiu). Quem enviou pode excluir; anexo de outra pessoa, só o Administrador.
    /// </summary>
    public async Task ExcluirAsync(Guid anexoId, CancellationToken ct = default)
    {
        await using var db = fabrica.Criar();
        var a = await db.Anexos.FirstOrDefaultAsync(x => x.Id == anexoId, ct) ?? throw new RegraDeNegocioException("Anexo não encontrado.");
        // Dono pelo id do usuário gravado na trilha de auditoria da inclusão (o nome de exibição pode repetir entre pessoas).
        var idTexto = a.Id.ToString();
        var donoId = await db.Auditoria.AsNoTracking()
            .Where(r => r.Entidade == nameof(Domain.Common.Anexo) && r.EntidadeId == idTexto && r.Operacao == "Inclusao")
            .Select(r => r.UsuarioId).FirstOrDefaultAsync(ct);
        var ehDono = fabrica.Usuario.UsuarioId is { } eu && donoId == eu.ToString();
        if (!ehDono) await Permissoes.ExigirAsync(db, fabrica.Usuario, "excluir anexo enviado por outra pessoa");
        db.Anexos.Remove(a);
        await db.SaveChangesAsync(ct);
    }

    public static IReadOnlyList<string> ValidarDestino(string entidade, Guid entidadeId)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(entidade)) e.Add("Registro de destino do anexo não informado.");
        if (entidadeId == Guid.Empty) e.Add("Salve o registro antes de anexar arquivos.");
        return e;
    }

    public static string? ValidarTamanho(string nome, long tamanho, long limiteBytes) =>
        tamanho > limiteBytes ? $"{nome}: limite de {limiteBytes / (1024 * 1024)} MB." : null;
}
