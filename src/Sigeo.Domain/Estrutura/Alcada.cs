using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>
/// Alçada de aprovação: define quem aprova o quê, em qual ordem e em qual faixa de valor.
/// Ex.: Requisição → 1º Engenharia, 2º Diretoria. Pedido até R$ 5 mil → Engenharia; acima → + Diretoria.
/// </summary>
public class Alcada : EntidadeAuditavel
{
    public EntidadeAprovavel Entidade { get; set; }
    /// <summary>Nulo = vale para todas as obras.</summary>
    public Guid? ObraId { get; set; }
    public int Ordem { get; set; }
    /// <summary>Rótulo da etapa exibido nas telas (ex.: "Aprov. Engenheiro", "Aprov. Diretor Técnico").</summary>
    public string? NomeEtapa { get; set; }
    public PerfilUsuario PerfilAprovador { get; set; }
    /// <summary>Opcional: aprovador nominal (sobrepõe o perfil).</summary>
    public Guid? UsuarioAprovadorId { get; set; }
    public decimal? ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }
    public string? Condicao { get; set; }
    public bool EngenheiroDaObra { get; set; }
    public Guid? SubstitutoUsuarioId { get; set; }
    public int? PrazoHoras { get; set; }
    public PerfilUsuario? EscalonaPara { get; set; }
    public bool Ativa { get; set; } = true;

    public bool SeAplica(decimal valor, Guid? obraId) =>
        Ativa && !Excluido
        && (ObraId is null || ObraId == obraId)
        && (ValorMinimo is null || valor >= ValorMinimo)
        && (ValorMaximo is null || valor <= ValorMaximo);
}
