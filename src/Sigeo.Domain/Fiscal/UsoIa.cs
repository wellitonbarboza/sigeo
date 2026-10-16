using Sigeo.Domain.Common;

namespace Sigeo.Domain.Fiscal;

/// <summary>Uma chamada de IA (modelo ou leitura de documento): base do limite mensal e do custo por construtora.</summary>
public class UsoIa : EntidadeAuditavel
{
    public string Funcao { get; set; } = string.Empty;
    public Guid? ReferenciaId { get; set; }
    public int TokensEntrada { get; set; }
    public int TokensSaida { get; set; }
    public int Paginas { get; set; }
    public bool Falhou { get; set; }
}
