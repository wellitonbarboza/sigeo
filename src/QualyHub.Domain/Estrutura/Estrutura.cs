using QualyHub.Domain.Common;

namespace QualyHub.Domain.Estrutura;

/// <summary>Empresa cliente (tenant). Ex.: N Dalmina. Permite multiempresa no futuro sem refatorar.</summary>
public class Empresa : Entidade
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    /// <summary>Nível pretendido no SiAC (ex.: "B"). Controla o recorte de serviços/materiais controlados.</summary>
    public string NivelSiac { get; set; } = "B";
    public bool Ativa { get; set; } = true;
}

public class Obra : EntidadeAuditavel, IOrigemObraPrima
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataPrevistaTermino { get; set; }
    public bool Ativa { get; set; } = true;
    public string? CodigoObraPrima { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    public ICollection<Local> Locais { get; set; } = [];
}

/// <summary>Hierarquia física da obra: Torre > Pavimento > Unidade/Ambiente. Usada nas FVS.</summary>
public class Local : EntidadeDaObra
{
    public Guid? LocalPaiId { get; set; }
    public Local? LocalPai { get; set; }
    public string Tipo { get; set; } = "Pavimento"; // Torre, Bloco, Pavimento, Unidade, Área comum
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public ICollection<Local> Filhos { get; set; } = [];
}

/// <summary>Usuário vinculado à identidade do Microsoft Entra ID (ObjectId).</summary>
public class Usuario : EntidadeAuditavel
{
    public Guid EntraObjectId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public bool Ativo { get; set; } = true;
    public ICollection<UsuarioObra> Obras { get; set; } = [];
    /// <summary>Limites de aprovação por tipo (e opcionalmente por obra) — "aprova até R$ X".</summary>
    public ICollection<Aprovacoes.LimiteAprovacao> LimitesAprovacao { get; set; } = [];
}

/// <summary>Quais obras cada usuário enxerga (base do filtro de acesso por obra).</summary>
public class UsuarioObra : Entidade
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Guid ObraId { get; set; }
    public Obra Obra { get; set; } = null!;
}

/// <summary>
/// Alçada de aprovação: define quem aprova o quê, em qual ordem e até qual valor.
/// Ex.: Pedido até R$ 5 mil → Engenharia; acima → Engenharia + Diretoria.
/// </summary>
public class Alcada : EntidadeAuditavel
{
    public EntidadeAprovavel Entidade { get; set; }
    /// <summary>Nulo = vale para todas as obras.</summary>
    public Guid? ObraId { get; set; }
    public int Ordem { get; set; }
    public PerfilUsuario PerfilAprovador { get; set; }
    /// <summary>Opcional: aprovador nominal (sobrepõe o perfil).</summary>
    public Guid? UsuarioAprovadorId { get; set; }
    public decimal? ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }
    public bool Ativa { get; set; } = true;

    public bool SeAplica(decimal valor, Guid? obraId) =>
        Ativa
        && (ObraId is null || ObraId == obraId)
        && (ValorMinimo is null || valor >= ValorMinimo)
        && (ValorMaximo is null || valor <= ValorMaximo);
}
