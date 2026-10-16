using Sigeo.Domain.Common;

namespace Sigeo.Contratos;

/// <summary>Quantidade com rótulo (linhas de painel: "RNC abertas: 3").</summary>
public sealed record ItemContagem(string Texto, int Quantidade);

/// <summary>Valor de um mês num gráfico de colunas (null = sem dado no mês, mostra "—", nunca zero).</summary>
public sealed record ValorMes(string Mes, decimal? Valor);

public sealed record FornecedorCritico(string Fornecedor, string Indicador, string Valor, string? Plano, bool PlanoAtrasado);

/// <summary>Painel de indicadores (SiAC 9.3): tudo calculado no servidor, a tela só desenha.</summary>
public sealed record PainelIndicadoresDto(
    decimal? LeadTimeDias, decimal? PctPropostas, decimal? PctFvm, decimal? NotaMedia, decimal? PctFvs1a, decimal MetaFvs1a,
    decimal? PctDocsEmDia, int AnaliseVencida,
    IReadOnlyList<ValorMes> FvmPorMes, IReadOnlyList<ValorMes> NotaPorMes,
    IReadOnlyList<FornecedorCritico> Piores, IReadOnlyList<ItemContagem> Documentos, IReadOnlyList<ItemContagem> QualidadeObra);

/// <summary>Painel SST (NBR 14280). Horas-homem: sugestão do servidor (ativos × 176 h × 12); a tela deixa o usuário trocar.</summary>
public sealed record PainelSstDto(
    int Ativos, int Aptos, decimal MetaAptos, int AcidentesComAfastamento12Meses, int? DiasSemAcidente,
    decimal HorasHomemSugeridas, decimal? ConformidadeInspecoes, decimal MetaInspecoes,
    IReadOnlyList<ItemContagem> PendenciasPorEmpresa, IReadOnlyList<ItemContagem> Vencendo30Dias);

/// <summary>Local da árvore da obra, só o necessário para desenhar (torre › pavimento › apartamento › cômodo).</summary>
public sealed record LocalMapa(Guid Id, Guid? PaiId, string Tipo, string Nome, string Codigo, int Ordem);

public sealed record ServicoMapa(Guid Id, string Codigo, string Sigla, string Nome);

/// <summary>Serviço controlado orçado num local (célula do mapa).</summary>
public sealed record CelulaMapa(Guid ServicoId, Guid LocalId);

/// <summary>FVS mais recente de um serviço num local (vale também para os locais abaixo dele).</summary>
public sealed record FvsMapa(Guid Id, string Numero, StatusFvs Status, Guid ServicoId, Guid LocalId);

/// <summary>
/// Dados do mapa da torre: a torre, seus ancestrais (FVS aberta acima vale para baixo), a subárvore e as áreas comuns
/// do condomínio. A situação de cada apartamento é calculada no navegador.
/// </summary>
public sealed record MapaTorreDto(Guid TorreId, IReadOnlyList<LocalMapa> Locais, IReadOnlyList<ServicoMapa> Servicos,
    IReadOnlyList<CelulaMapa> Celulas, IReadOnlyList<FvsMapa> Fvs);
