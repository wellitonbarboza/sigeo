using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

/// <summary>
/// Funcionário próprio ou de terceiro. LGPD (minimização): guarda apenas o necessário para controle
/// de acesso à obra e treinamentos — sem dados de saúde, endereço residencial ou dados bancários.

/// <summary>Serviço controlado (lista de serviços controlados / ITS). Liga contrato, medição e FVS.</summary>
public class ServicoControlado : EntidadeAuditavel, IOrigemErp
{
    /// <summary>Ex.: ITS 03.02</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    /// <summary>Sigla curta exibida no mapa da torre (ex.: ALV, CTP, PIN).</summary>
    public string? Sigla { get; set; }
    /// <summary>Documento do SGQ (ITS) que descreve o serviço — enviado ao prestador junto com o contrato.</summary>
    public Guid? DocumentoItsId { get; set; }
    public bool ExigeFvsParaMedicao { get; set; } = true;
    public bool AvaliadoNaMedicao { get; set; } = true;
    public bool Ativo { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
}
