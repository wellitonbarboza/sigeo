using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Sigeo.Application.Comum;
using Sigeo.Domain.Fiscal;

namespace Sigeo.Application.Fiscal;

/// <summary>Resultado da leitura de um XML: nota (NF-e modelo 55), cancelamento (evento 110111) ou arquivo ignorado.</summary>
public abstract record XmlLido(string Arquivo);

public sealed record NotaLida(string Arquivo, NotaFiscalEntrada Nota, string Xml) : XmlLido(Arquivo);

public sealed record CancelamentoLido(string Arquivo, string Chave) : XmlLido(Arquivo);

public sealed record XmlIgnorado(string Arquivo, string Motivo) : XmlLido(Arquivo);
