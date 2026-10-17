using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Cadastros;
using Sigeo.Domain.Common;
using Sigeo.Domain.Regras;

namespace Sigeo.Application.Cadastros;

/// <summary>Painel do fornecedor (ficha única): documentos, mensais, qualificação, desempenho e uso.</summary>
public sealed record PainelFornecedor(int DocsValidos, int DocsPendentes, int DocsVencidos, int DocsEmValidacao, IReadOnlyList<string> MensaisPendentes,
    IReadOnlyList<string> Bloqueios, int Qualificacoes, DateOnly? QualificacaoValidaAte, int Fvm, decimal? FvmAprovadasPerc, decimal? NotaMedia,
    int Pedidos, decimal ValorPedidos, int Contratos, decimal ValorContratos, int PlanosAbertos);
