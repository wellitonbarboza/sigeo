using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Cadastros;

public class Fornecedor : EntidadeAuditavel, IOrigemErp, IHistoricoTemporal
{
    public string CpfCnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Telefones { get; set; }
    /// <summary>Obrigatório para envio de pedido/contrato (P.SUP 3.2.1).</summary>
    public string? Emails { get; set; }
    public string? NaturezaJuridica { get; set; }
    public string? Porte { get; set; }
    public string? SituacaoCadastral { get; set; }
    public string? Cnae { get; set; }
    public decimal? CapitalSocial { get; set; }
    public DateOnly? DataFundacao { get; set; }
    public string? Telefone0800 { get; set; }
    public string? Site { get; set; }
    public bool OptanteSimples { get; set; }
    public bool Mei { get; set; }
    /// <summary>Sistema de origem do cadastro (ex.: "ExpertData", "ERP") e trilha de quem criou/alterou lá.</summary>
    public string? OrigemSistema { get; set; }
    public string? OrigemCriadoPor { get; set; }
    public DateTimeOffset? OrigemCriadoEm { get; set; }
    public string? OrigemAlteradoPor { get; set; }
    public DateTimeOffset? OrigemAlteradoEm { get; set; }
    public TipoFornecimento TipoFornecimento { get; set; }
    public StatusQualificacao StatusQualificacao { get; set; } = StatusQualificacao.EmQualificacao;
    /// <summary>Desqualificado = inativo no sistema.</summary>
    public bool Ativo { get; set; } = true;
    /// <summary>Média das notas (medições + FVM) dos últimos 6 meses — recalculada pelo sistema.</summary>
    public decimal? NotaMedia { get; set; }
    public string? CodigoErp { get; set; }
    public Guid? UltimaImportacaoId { get; set; }

    /// <summary>Pré-cadastro: criado pela empresa com o mínimo; o fornecedor completa pelo link do portal.</summary>
    public bool PreCadastro { get; set; }
    /// <summary>Exigir documentos dos funcionários (prestador que coloca pessoas na obra).</summary>
    public bool IncluirDocsFuncionarios { get; set; }

    public ICollection<FornecedorDocumento> Documentos { get; set; } = [];
    public ICollection<FornecedorQualificacao> Qualificacoes { get; set; } = [];
    public ICollection<FornecedorContato> Contatos { get; set; } = [];
    /// <summary>Grupos do catálogo que o fornecedor atende (subgrupos opcionais para separar fornecedores).</summary>
    public ICollection<FornecedorGrupo> Grupos { get; set; } = [];
    /// <summary>Serviços (ITS) que o fornecedor executa.</summary>
    public ICollection<FornecedorServico> Servicos { get; set; } = [];
    /// <summary>Documentos exigidos deste fornecedor (escolhidos no pré-cadastro a partir da matriz).</summary>
    public ICollection<FornecedorExigencia> Exigencias { get; set; } = [];

    public string NomeExibicao => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia!;

    /// <summary>Qualificação vigente para o escopo informado (material controlado ou serviço), na data.</summary>
    public FornecedorQualificacao? QualificacaoVigente(DateOnly data, Guid? materialId = null, Guid? servicoId = null) =>
        Qualificacoes.Where(q => !q.Excluido && q.Resultado == ResultadoQualificacao.Qualificado && q.DataQualificacao <= data)
                     .Where(q => materialId is null || q.MaterialControladoId is null || q.MaterialControladoId == materialId)
                     .Where(q => servicoId is null || q.ServicoControladoId is null || q.ServicoControladoId == servicoId)
                     .OrderByDescending(q => q.DataQualificacao)
                     .FirstOrDefault(q => q.DataValidade is null || q.DataValidade >= data);

    /// <summary>Status exibido no cadastro, derivado das qualificações e da decisão semestral.</summary>
    public StatusQualificacao CalcularStatus(DateOnly hoje)
    {
        if (StatusQualificacao == StatusQualificacao.Desqualificado) return StatusQualificacao.Desqualificado;
        var qs = Qualificacoes.Where(q => !q.Excluido).OrderByDescending(q => q.DataQualificacao).ToList();
        if (qs.Count == 0) return StatusQualificacao.EmQualificacao;
        if (QualificacaoVigente(hoje) is not null) return StatusQualificacao.Qualificado;
        var ultima = qs[0];
        return ultima.Resultado switch
        {
            ResultadoQualificacao.Qualificado => StatusQualificacao.Vencida,
            ResultadoQualificacao.NaoQualificado => StatusQualificacao.NaoQualificado,
            _ => StatusQualificacao.EmQualificacao
        };
    }
}
