using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

/// <summary>Empresa (tenant) — matriz, filial ou SPE. Permite multiempresa sem refatorar.</summary>
public class Empresa : Entidade
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    /// <summary>Código curto exibido nas telas (ex.: F001, SPE-01).</summary>
    public string? Codigo { get; set; }
    public string? EnderecoEntregaPadrao { get; set; }
    /// <summary>Nível pretendido no SiAC (A ou B). Controla o recorte de serviços/materiais controlados.</summary>
    public string NivelSiac { get; set; } = "B";
    /// <summary>Falsa = construtora suspensa pela plataforma: ninguém dela entra; os dados ficam guardados.</summary>
    public bool Ativa { get; set; } = true;
    public DateTimeOffset? CriadaEm { get; set; }
    /// <summary>Modelo de estrutura de locais (ver <see cref="EstruturaLocais"/>): Condominio, Edificio, Loteamento ou Personalizado.</summary>
    public string? ModeloEstrutura { get; set; }
    /// <summary>Nomes dos níveis separados por ";": condomínio;torre;pavimento;unidade;cômodo;prefixo da unidade.</summary>
    public string? NomesNiveis { get; set; }
    /// <summary>Telas que a plataforma não liberou para a construtora (rotas separadas por ";"). Vazio = tudo liberado.</summary>
    public string? TelasBloqueadas { get; set; }
}
