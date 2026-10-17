using Sigeo.Domain.Common;

namespace Sigeo.Domain.Estrutura;

public class Obra : EntidadeAuditavel, IOrigemErp
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataPrevistaTermino { get; set; }
    public string? Observacoes { get; set; }
    public string? EnderecoEntrega { get; set; }
    public Guid? EngenheiroId { get; set; }
    public Guid? AdministrativoId { get; set; }
    public Guid? AlmoxarifeId { get; set; }
    public Guid? PqoDocumentoId { get; set; }
    public StatusObra Status { get; set; } = StatusObra.EmExecucao;
    public bool Ativa { get; set; } = true;
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }
    /// <summary>Obra própria: o custo fica na construtora (Matriz/Filial), sem SPE. Quando a SPE for constituída, os locais e os documentos em aberto passam para ela.</summary>
    public DonoCustoObra DonoCusto { get; set; } = DonoCustoObra.Spe;
    /// <summary>Estabelecimento (Matriz/Filial) dono do custo na obra própria — vira o "Faturar para" padrão.</summary>
    public Guid? EstabelecimentoDonoId { get; set; }

    /// <summary>
    /// Data de corte do sistema: antes dela os dados vêm do ERP como histórico (só leitura, sem aprovações nem registros de qualidade);
    /// a partir dela tudo é lançado no SIGEO com os fluxos completos.
    /// </summary>
    public DateOnly? DataCorteSistema { get; set; }

    public bool ObraPropria => DonoCusto == DonoCustoObra.Construtora;

    public ICollection<Local> Locais { get; set; } = [];
}
