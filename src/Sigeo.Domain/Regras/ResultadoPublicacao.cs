using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;
using Sigeo.Domain.Estrutura;

namespace Sigeo.Domain.Regras;

/// <summary>Resultado da publicação: o que o sistema gerou/alterou (para log e avisos).</summary>
public sealed record ResultadoPublicacao(DocumentoRevisao? Anterior, IReadOnlyList<CopiaControlada> CopiasParaRecolher, IReadOnlyList<Ciencia> Ciencias);
