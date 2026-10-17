using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sigeo.Application.Abstracoes;
using Sigeo.Application.Fiscal;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.IA;

public sealed record SugestaoRnc(string Resumo, IReadOnlyList<string> CausasProvaveis, IReadOnlyList<string> CincoPorques, string Disposicao,
    string AcaoCorretiva, string AcaoPreventiva, int PrazoDias, IReadOnlyList<string> Referencias);

public sealed record SugestaoProduto(string? Grupo, string? SubGrupo, string? Unidade, IReadOnlyList<string> PossiveisDuplicados, string? Observacao);

public sealed record ResultadoLaudo(string? Tipo, string? Emitente, string? Numero, DateOnly? DataEmissao, DateOnly? Validade, string? Lote, string? Material,
    IReadOnlyList<ResultadoEnsaioLido> Resultados, bool? Conforme, string? Conclusao, int Paginas);

public sealed record ResultadoEnsaioLido(string Ensaio, string? Valor, string? Unidade, string? Especificacao, bool? Conforme);

/// <summary>Limite de chamadas de IA por construtora no mês (controle de custo; configuração IA:LimiteMensalPorEmpresa).</summary>
public sealed record LimitesIa(int ChamadasPorMes);
