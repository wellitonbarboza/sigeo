using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Qualidade;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Domain.Regras;

public static class StatusDocumentoCalculo
{
    public static StatusDocumento Calcular(DateOnly? validade, bool temArquivo, int diasAlerta, DateOnly hoje)
    {
        if (!temArquivo) return StatusDocumento.PendenteEnvio;
        if (validade is null) return StatusDocumento.Valido;
        if (validade.Value < hoje) return StatusDocumento.Vencido;
        return validade.Value <= hoje.AddDays(diasAlerta) ? StatusDocumento.AVencer : StatusDocumento.Valido;
    }

    public static StatusDocumento Calcular(FornecedorDocumento d, DateOnly hoje) =>
        d.Status is StatusDocumento.Reprovado or StatusDocumento.EmValidacao ? d.Status
        : Calcular(d.DataValidade, d.ArquivoBlob is not null, d.TipoDocumento?.DiasAlertaAntesVencimento ?? 30, hoje);
}
