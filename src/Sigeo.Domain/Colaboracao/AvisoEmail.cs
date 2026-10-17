using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>
/// Fila de avisos por e-mail (saída das regras de notificação). Gravar na fila junto com o fato que gerou o aviso
/// garante que nada se perde se o envio falhar: a rotina de envio tenta de novo (até <see cref="MaxTentativas"/>).
/// A chave evita repetir o mesmo aviso (ex.: aprovação pendente lembrada uma vez por dia).
/// </summary>
public class AvisoEmail : EntidadeAuditavel
{
    public const int MaxTentativas = 5;

    public string Evento { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public Guid? UsuarioId { get; set; }
    public string Para { get; set; } = string.Empty;
    public string NomeDestinatario { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    /// <summary>Caminho relativo da tela (ex.: "obra/rnc?id=…"); o endereço do site é acrescentado no envio.</summary>
    public string? Link { get; set; }
    public SituacaoAviso Situacao { get; set; } = SituacaoAviso.Pendente;
    public int Tentativas { get; set; }
    public DateTimeOffset? ProximaTentativaEm { get; set; }
    public DateTimeOffset? EnviadoEm { get; set; }
    public string? Detalhe { get; set; }

    public void MarcarEnviado(DateTimeOffset agora) { Situacao = SituacaoAviso.Enviado; EnviadoEm = agora; Detalhe = null; }

    /// <summary>Falha de envio: espera cresce a cada tentativa (1, 4, 9, 16 min); na última fica como Falhou.</summary>
    public void MarcarFalha(string erro, DateTimeOffset agora)
    {
        Tentativas++;
        Detalhe = erro.Length > 1000 ? erro[..1000] : erro;
        if (Tentativas >= MaxTentativas) { Situacao = SituacaoAviso.Falhou; ProximaTentativaEm = null; return; }
        ProximaTentativaEm = agora.AddMinutes(Tentativas * Tentativas);
    }

    /// <summary>Troca {campo} pelos valores (campo sem valor fica como está, para quem configura a regra perceber).</summary>
    public static string Preencher(string modelo, IReadOnlyDictionary<string, string?> valores)
    {
        var s = modelo ?? "";
        foreach (var (k, v) in valores) s = s.Replace("{" + k + "}", v ?? "", StringComparison.OrdinalIgnoreCase);
        return s;
    }
}
