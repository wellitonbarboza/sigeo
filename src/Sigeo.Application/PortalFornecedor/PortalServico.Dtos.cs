using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Comum;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>Documento pedido ao fornecedor no portal (tela 24).</summary>
public sealed record DocumentoSolicitado(Guid TipoId, string Nome, bool PorEntrega, bool Obrigatorio, StatusDocumento Situacao, DateOnly? Validade, string? Detalhe)
{
    public FrequenciaDocumento Frequencia { get; init; } = FrequenciaDocumento.Validade;
    /// <summary>Mensal: competência esperada (mês anterior).</summary>
    public DateOnly? Competencia { get; init; }
    public string? Categoria { get; init; }
}

public sealed record SessaoPortal(Guid EmpresaId, string EmpresaNome, Fornecedor Fornecedor, LinkPortalFornecedor Link, IReadOnlyList<DocumentoSolicitado> Documentos, string? Alerta)
{
    public IReadOnlyList<ColaboradorPortal> Colaboradores { get; init; } = [];
    public IReadOnlyList<(Guid Id, string Nome)> Exigencias { get; init; } = [];
    /// <summary>Contratos de mão de obra ativos do fornecedor (obras onde os funcionários trabalham).</summary>
    public IReadOnlyList<ContratoPortal> Contratos { get; init; } = [];
    public IReadOnlyList<(Guid Id, string Nome)> Funcoes { get; init; } = [];
    public bool MaoDeObra => Contratos.Count > 0 || Fornecedor.TipoFornecimento.HasFlag(TipoFornecimento.ServicoObra);
}

public sealed record ContratoPortal(Guid Id, string Numero, Guid ObraId, string Obra);

/// <summary>Pendência mostrada no Início do portal (ordem de urgência: 1 = mais urgente).</summary>
public sealed record PendenciaPortal(int Urgencia, string Selo, string Tom, string Titulo, string Detalhe, string Aba, Guid? Id = null);

public sealed record InicioPortal(string Situacao, string Tom, string Motivo, int CotacoesAbertas, string? CotacaoUrgente, int DocsValidos, int DocsTotal, int DocsVencendo, int DocsRecusados,
    int Funcionarios, int FuncionariosPendencia, int FuncionariosAguardando, string? Competencia, int CompetenciaEntregues, int CompetenciaTotal, IReadOnlyList<PendenciaPortal> Pendencias,
    IReadOnlyList<string> EmailsAvisos);

public sealed record PedidoPortal(Guid Id, int Numero, string Obra, DateOnly Data, DateOnly Entrega, StatusPedido Status, decimal Valor, IReadOnlyList<(string Item, string Unidade, decimal Pedido, decimal Recebido)> Itens);

public sealed record VersaoDocumentoPortal(Guid Id, DateTimeOffset Enviado, string? Responsavel, DateOnly? Validade, StatusDocumento Status, string? Motivo, string? Arquivo, bool Vigente);

public sealed class ColaboradorForm
{
    public Guid? Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Cpf { get; set; }
    public Guid FuncaoId { get; set; }
    public DateOnly Admissao { get; set; }
    public Guid? ContratoId { get; set; }
    public DateOnly? Desligamento { get; set; }
}

public sealed record CompetenciaPortal(Guid Id, Guid ContratoId, string Contrato, string Obra, DateOnly Mes, SituacaoCompetencia Situacao, bool Atrasada, int Entregues, int Total, DateOnly Limite);

/// <summary>Proposta de cotação preenchida pelo fornecedor no portal (link da cotação, sem login).</summary>
public sealed record PropostaPortal(string EmpresaNome, string FornecedorNome, int CotacaoNumero, string CotacaoNome, string? EnderecoEntrega,
    TipoFrete Frete, bool Aberta, CotacaoFornecedor Convite, IReadOnlyList<CotacaoItem> Itens, string? FaturarPara = null, string? Destino = null)
{
    /// <summary>Quebra por local de cada item da proposta (torre › pavimento › apto › cômodo): item → linhas.</summary>
    public IReadOnlyDictionary<Guid, List<Comum.LocalQuantidade>> Locais { get; init; } = new Dictionary<Guid, List<Comum.LocalQuantidade>>();
    /// <summary>Cotação de material: a marca de cada item cotado é obrigatória.</summary>
    public bool Material { get; init; }
    /// <summary>Marca pedida na requisição por item da proposta (e se aceita similar).</summary>
    public IReadOnlyDictionary<Guid, (string Marca, bool Somente)> MarcasPedidas { get; init; } = new Dictionary<Guid, (string, bool)>();
    public DateTimeOffset? PrazoResposta { get; init; }
    public RascunhoProposta? Rascunho { get; init; }
    public IReadOnlyList<PropostaVersao> Versoes { get; init; } = [];
    public SituacaoPropostaFornecedor.Resultado? Situacao { get; init; }
}

public sealed record RascunhoProposta(IReadOnlyList<ItemPropostaPortal> Itens, decimal? Frete, string? Condicao, DateOnly? Validade, DateOnly? Prazo);

/// <summary>Dados cadastrais que o fornecedor completa pelo portal (pré-cadastro).</summary>
public sealed class DadosPortal
{
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
    public string? Emails { get; set; }
    public string? Site { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoEmail { get; set; }
    public string? ContatoTelefone { get; set; }
}

public sealed record ItemPropostaPortal(Guid ItemId, decimal ValorUnitario, DateOnly? Entrega, string? Marca = null);

/// <summary>Funcionário da empresa terceira visto no portal (aba Funcionários): só nome, função e pendências — sem CPF.</summary>
public sealed record ColaboradorPortal(Guid Id, string Nome, string Funcao, string Situacao, string? Pendencias)
{
    public string? CpfMascarado { get; init; }
    public Guid FuncaoId { get; init; }
    public DateOnly Admissao { get; init; }
    public DateOnly? Desligamento { get; init; }
    public Guid? ContratoId { get; init; }
    public string? Obra { get; init; }
    public bool AguardandoValidacao { get; init; }
}
