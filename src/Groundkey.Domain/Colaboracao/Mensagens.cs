using Groundkey.Domain.Aprovacoes;
using Groundkey.Domain.Common;

namespace Groundkey.Domain.Colaboracao;

/// <summary>Papel de quem participa da conversa (quem abriu, para, cópia, ou entrou depois respondendo/sendo incluído).</summary>
public enum PapelParticipante { Remetente = 0, Para = 1, Copia = 2 }

/// <summary>
/// Conversa interna (caixa de mensagens entre usuários do Groundkey). Pode ficar ligada a um registro
/// (RNC, pedido, cotação, contrato, versão do cronograma…) para servir de evidência junto dele.
/// Nada é apagado: a conversa só é arquivada por quem quiser tirá-la da entrada; o texto enviado não se edita.
/// </summary>
public class Conversa : EntidadeAuditavel, IHistoricoTemporal
{
    public const int TamanhoAssunto = 200;
    public const int TamanhoTexto = 20000;
    public const int MaxParticipantes = 200;

    public int Numero { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public Guid? ObraId { get; set; }
    /// <summary>Registro ligado (nome da entidade, ex.: "Rnc"), com id e descrição gravada no envio (ex.: "RNC 0012").</summary>
    public string? RegistroEntidade { get; set; }
    public Guid? RegistroId { get; set; }
    public string? RegistroDescricao { get; set; }
    public Guid RemetenteId { get; set; }
    public string RemetenteNome { get; set; } = string.Empty;
    public DateTimeOffset UltimaEm { get; set; }
    public ICollection<ParticipanteConversa> Participantes { get; set; } = [];
    public ICollection<MensagemConversa> Mensagens { get; set; } = [];

    /// <summary>Abre a conversa com a primeira mensagem. Destinatários repetidos ou o próprio remetente entram uma vez só.</summary>
    public static Conversa Abrir(string assunto, string texto, Guid remetenteId, string remetenteNome,
        IReadOnlyCollection<(Guid Id, string Nome)> para, IReadOnlyCollection<(Guid Id, string Nome)> copia, DateTimeOffset agora)
    {
        var a = (assunto ?? "").Trim();
        var erros = new List<string>();
        if (a.Length == 0) erros.Add("Informe o assunto.");
        if (a.Length > TamanhoAssunto) erros.Add($"Assunto com mais de {TamanhoAssunto} caracteres.");
        if (para.Count == 0) erros.Add("Informe ao menos um destinatário.");
        ValidarTexto(texto, erros);
        if (remetenteId == Guid.Empty) erros.Add("Remetente não identificado.");
        if (erros.Count > 0) throw new RegraDeNegocioException(string.Join(" ", erros));

        var c = new Conversa { Assunto = a, RemetenteId = remetenteId, RemetenteNome = remetenteNome, UltimaEm = agora };
        c.Participantes.Add(new ParticipanteConversa { ConversaId = c.Id, UsuarioId = remetenteId, Nome = remetenteNome, Papel = PapelParticipante.Remetente, LidaAte = agora });
        foreach (var (id, nome) in para) c.Incluir(id, nome, PapelParticipante.Para);
        foreach (var (id, nome) in copia) c.Incluir(id, nome, PapelParticipante.Copia);
        if (c.Participantes.Count == 1) throw new RegraDeNegocioException("Informe ao menos um destinatário além de você.");
        if (c.Participantes.Count > MaxParticipantes) throw new RegraDeNegocioException($"Máximo de {MaxParticipantes} participantes por conversa.");
        c.Mensagens.Add(new MensagemConversa { ConversaId = c.Id, AutorId = remetenteId, AutorNome = remetenteNome, Texto = texto.Trim(), EnviadaEm = agora });
        return c;
    }

    public void LigarRegistro(string entidade, Guid id, string descricao)
    {
        if (string.IsNullOrWhiteSpace(entidade) || id == Guid.Empty) return;
        RegistroEntidade = entidade; RegistroId = id; RegistroDescricao = descricao;
    }

    /// <summary>Participante novo (ex.: incluído numa resposta). Quem já participa não duplica.</summary>
    public ParticipanteConversa? Incluir(Guid usuarioId, string nome, PapelParticipante papel)
    {
        if (usuarioId == Guid.Empty || Participantes.Any(p => p.UsuarioId == usuarioId)) return null;
        var p = new ParticipanteConversa { ConversaId = Id, UsuarioId = usuarioId, Nome = nome, Papel = papel };
        Participantes.Add(p);
        return p;
    }

    /// <summary>Resposta de um participante. A conversa volta para a entrada de quem tinha arquivado.</summary>
    public MensagemConversa Responder(Guid autorId, string autorNome, string texto, DateTimeOffset agora)
    {
        var p = Participantes.FirstOrDefault(x => x.UsuarioId == autorId)
                ?? throw new RegraDeNegocioException("Só quem participa da conversa pode responder.");
        var erros = new List<string>();
        ValidarTexto(texto, erros);
        if (erros.Count > 0) throw new RegraDeNegocioException(string.Join(" ", erros));
        var m = new MensagemConversa { ConversaId = Id, AutorId = autorId, AutorNome = autorNome, Texto = texto.Trim(), EnviadaEm = agora };
        Mensagens.Add(m);
        UltimaEm = agora;
        p.LidaAte = agora;
        foreach (var o in Participantes.Where(x => x.UsuarioId != autorId)) o.Arquivada = false;
        return m;
    }

    private static void ValidarTexto(string? texto, List<string> erros)
    {
        var t = (texto ?? "").Trim();
        if (t.Length == 0) erros.Add("Escreva a mensagem.");
        if (t.Length > TamanhoTexto) erros.Add($"Mensagem com mais de {TamanhoTexto:N0} caracteres.");
    }
}

/// <summary>Quem participa da conversa e o estado dela para essa pessoa (arquivada, até quando leu).</summary>
public class ParticipanteConversa : EntidadeAuditavel
{
    public Guid ConversaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public PapelParticipante Papel { get; set; }
    public bool Arquivada { get; set; }
    /// <summary>Até quando a pessoa leu (mensagens enviadas depois disso contam como não lidas).</summary>
    public DateTimeOffset? LidaAte { get; set; }
}

/// <summary>Mensagem enviada dentro da conversa. Imutável: correção = nova mensagem.</summary>
public class MensagemConversa : Entidade
{
    public Guid ConversaId { get; set; }
    public Guid AutorId { get; set; }
    public string AutorNome { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTimeOffset EnviadaEm { get; set; }
    public ICollection<LeituraMensagem> Leituras { get; set; } = [];
}

/// <summary>"Lido por quem e quando": primeira leitura de cada pessoa em cada mensagem (evidência).</summary>
public class LeituraMensagem : Entidade
{
    public Guid MensagemId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTimeOffset LidaEm { get; set; }
}

/// <summary>
/// Texto formatado das mensagens sem HTML: **negrito**, _itálico_, linhas iniciadas por "- " viram lista.
/// O texto é sempre codificado antes de aplicar a formatação — nada que o usuário digite vira HTML ativo.
/// </summary>
public static class TextoFormatado
{
    public static string ParaHtml(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var sb = new System.Text.StringBuilder();
        var emLista = false;
        foreach (var bruta in texto.Replace("\r\n", "\n").Split('\n'))
        {
            var linha = bruta.TrimEnd();
            var item = linha.StartsWith("- ") || linha.StartsWith("• ");
            if (item && !emLista) { sb.Append("<ul>"); emLista = true; }
            if (!item && emLista) { sb.Append("</ul>"); emLista = false; }
            var conteudo = Inline(Codificar(item ? linha[2..] : linha));
            if (item) sb.Append("<li>").Append(conteudo).Append("</li>");
            else if (linha.Length == 0) sb.Append("<br>");
            else sb.Append("<p>").Append(conteudo).Append("</p>");
        }
        if (emLista) sb.Append("</ul>");
        return sb.ToString();
    }

    /// <summary>Codifica só o que importa para o HTML (&amp; &lt; &gt; aspas); acentos ficam legíveis.</summary>
    private static string Codificar(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");

    private static string Inline(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\*\*(.+?)\*\*", "<b>$1</b>");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"(?<![\w])_(.+?)_(?![\w])", "<i>$1</i>");
        return s;
    }

    /// <summary>Resumo de uma linha (lista, e-mail, prévia): sem marcações.</summary>
    public static string Resumo(string? texto, int max = 120)
    {
        var t = System.Text.RegularExpressions.Regex.Replace((texto ?? "").Replace("**", "").Replace("\n- ", " · ").Replace('\n', ' '), @"\s+", " ").Trim();
        return t.Length <= max ? t : t[..(max - 1)].TrimEnd() + "…";
    }
}
