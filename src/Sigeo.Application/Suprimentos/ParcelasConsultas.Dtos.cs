using Sigeo.Domain.Common;
using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Contrato recorrente no filtro da tela de parcelas.</summary>
public sealed record ContratoRecorrenteResumo(Guid Id, string Numero, string Fornecedor, FormaCobranca FormaCobranca, decimal Tolerancia);

/// <summary>Linha da grade de parcelas (com o contrato e se a nota está fora da tolerância).</summary>
public sealed record ParcelaLinha(ParcelaContrato P, string Contrato, string Fornecedor, bool Fora);

/// <summary>Contratos recorrentes e parcelas da grade.</summary>
public sealed record ParcelasLista(List<ContratoRecorrenteResumo> Contratos, List<ParcelaLinha> Linhas);

/// <summary>Parcela aberta com contrato e usos do período.</summary>
public sealed record ParcelaAberta(ParcelaContrato Parcela, Contrato Contrato, List<UsoContrato> Usos);
