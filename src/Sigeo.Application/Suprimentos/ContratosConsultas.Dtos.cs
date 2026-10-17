using Sigeo.Domain.Common;
using Sigeo.Domain.Estrutura;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Linha da grade de contratos.</summary>
public sealed record ContratoLinha(Guid Id, string Numero, string Obra, string Fornecedor, string Categoria, string Objeto, decimal Valor, DateOnly Fim, StatusContrato Status, string CriadoPor);

/// <summary>Contrato aberto e o que a tela mostra junto (uma ida ao banco).</summary>
/// <param name="LimiteAditivoPerc">Parâmetro de limite de aditivo (%).</param>
public sealed record ContratoAberto(Contrato Contrato, List<Local> Locais, List<Medicao> Medicoes, List<Aditivo> Aditivos, decimal LimiteAditivoPerc);

/// <summary>Aditivo aberto e os aditivos do mesmo contrato.</summary>
public sealed record AditivoAberto(Aditivo Aditivo, List<Aditivo> Aditivos);
