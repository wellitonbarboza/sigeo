using Sigeo.Domain.Common;

namespace Sigeo.Application.Cadastros;

/// <summary>Linha da grade de qualificações.</summary>
public sealed record QualificacaoLinha(Guid Id, string Fornecedor, TipoFornecimento Tipo, string? Escopo, string Criterio, SituacaoPsq Psq, DateOnly Data, DateOnly? Validade,
    ResultadoQualificacao Resultado, string? Responsavel, string CriadoPor);
