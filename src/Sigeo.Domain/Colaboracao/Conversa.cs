using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Colaboracao;

/// <summary>
/// Conversa interna (caixa de mensagens entre usuários do SIGEO). Pode ficar ligada a um registro
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
