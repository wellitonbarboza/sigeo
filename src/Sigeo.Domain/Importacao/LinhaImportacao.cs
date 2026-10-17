using Sigeo.Domain.Common;

namespace Sigeo.Domain.Importacao;

public class LinhaImportacao : Entidade
{
    public Guid LoteImportacaoId { get; set; }
    public int NumeroLinha { get; set; }
    public string ChaveNatural { get; set; } = string.Empty;   // CNPJ, nº pedido, nº medição...
    public string DadosJson { get; set; } = "{}";
    public AcaoLinhaImportacao Acao { get; set; }
    public string? Erros { get; set; }
    public Guid? EntidadeAfetadaId { get; set; }
}
