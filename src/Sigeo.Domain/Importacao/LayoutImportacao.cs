using Sigeo.Domain.Common;

namespace Sigeo.Domain.Importacao;

/// <summary>
/// Layout de colunas do export do ERP (mapeamento salvo para não remapear a cada arquivo).
/// MapeamentoJson: { "CpfCnpj": "CNPJ/CPF", "RazaoSocial": "Razão Social", ... } (campo SIGEO → cabeçalho do arquivo)
/// </summary>
public class LayoutImportacao : EntidadeAuditavel
{
    public TipoImportacao Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int LinhaCabecalho { get; set; } = 1;
    public string? NomePlanilha { get; set; }
    public string Delimitador { get; set; } = ";";
    public string MapeamentoJson { get; set; } = "{}";
    public bool Padrao { get; set; }
}
