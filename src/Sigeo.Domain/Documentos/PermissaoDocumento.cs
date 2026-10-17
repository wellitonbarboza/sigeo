using System.Security.Cryptography;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Documentos;

/// <summary>D07 · Permissão por pasta ou documento, por perfil ou usuário (documento sobrepõe pasta).</summary>
public class PermissaoDocumento : EntidadeAuditavel, IHistoricoTemporal
{
    public EscopoPermissao Escopo { get; set; }
    public Guid AlvoId { get; set; }
    public PerfilUsuario? Perfil { get; set; }
    public Guid? UsuarioId { get; set; }
    public bool PodeVer { get; set; } = true;
    public bool PodeBaixar { get; set; }
    public bool PodeImprimir { get; set; }
    public bool PodeEditar { get; set; }
    public bool PodeAprovar { get; set; }
    public bool PodeVerObsoletos { get; set; }
    public DateOnly? ValidoAte { get; set; }

    public bool Vigente(DateOnly hoje) => !Excluido && (ValidoAte is null || ValidoAte >= hoje);
}
