using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.PortalFornecedor;

/// <summary>Contato que pode receber o código (dados mascarados: a tela é pública).</summary>
public sealed record ContatoAcesso(Guid ContatoId, string Nome, string? Email, string? Celular)
{
    public bool TemEmail => Email is not null;
    public bool TemCelular => Celular is not null;
}

/// <summary>Quem entrou: o documento digitado e o destino (e-mail/celular) que recebeu e devolveu o código.</summary>
public sealed record IdentidadeFornecedor(string Documento, string DestinoVerificado);

/// <summary>CNPJ (fornecedor) numa construtora que a pessoa pode representar.</summary>
public sealed record AcessoCnpj(Guid EmpresaId, string Construtora, Guid FornecedorId, string RazaoSocial, string CpfCnpj, int CotacoesAbertas);

/// <summary>Cotação para a qual o fornecedor foi convidado, com a situação da proposta dele.</summary>
public sealed record CotacaoDoFornecedor(Guid CotacaoId, int Numero, string Nome, string? Obra, string Situacao, bool Enviada, bool PodeEditar, DateOnly? DataResposta)
{
    public SituacaoProposta Codigo { get; init; }
    public string Tom { get; init; } = "neutro";
    public DateTimeOffset? Prazo { get; init; }
    public DateTimeOffset? CriadaEm { get; init; }
    /// <summary>Pedido(s)/contrato(s) gerados para este fornecedor a partir da cotação.</summary>
    public string? Resultado { get; init; }
    public int Versoes { get; init; }
    public decimal? Total { get; init; }
    public TipoRequisicao Tipo { get; init; }
    public int Itens { get; init; }
    public string? MotivoDeclinio { get; init; }
}
