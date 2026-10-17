using System.Text.RegularExpressions;

namespace Sigeo.Domain.Fiscal;

/// <summary>Partes da chave de acesso da NF-e (44 dígitos), na ordem do leiaute da SEFAZ.</summary>
public sealed record PartesChaveNfe(string Uf, int Ano, int Mes, string CnpjEmitente, string Modelo, string Serie, int Numero);
