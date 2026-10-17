using System.Globalization;
using System.Text;
using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

/// <summary>Registro de cada busca de notas (SIEG ou arquivo): o que veio, o que entrou e o erro, se houve.</summary>
public class ImportacaoNota : EntidadeAuditavel
{
    public OrigemNota Origem { get; set; }
    public string? Cnpj { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public int Lidas { get; set; }
    public int Novas { get; set; }
    public int Canceladas { get; set; }
    public string? Detalhe { get; set; }
    public bool Falhou { get; set; }
}
