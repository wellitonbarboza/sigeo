using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Aprovacoes;
using Sigeo.Domain.Common;
using Sigeo.Domain.Documentos;

namespace Sigeo.Application.Documentos;

/// <summary>Código, título e revisão lidos do conteúdo (ou do nome) de um arquivo do SGQ.</summary>
public sealed record IdentificacaoArquivoSgq(string Arquivo, string? Codigo, string? Titulo, int? Revisao, string? DescricaoRevisao, string? Fonte);

/// <summary>Arquivo enviado na importação em lote, já identificado.</summary>
public sealed record ArquivoLoteSgq(string Nome, byte[] Conteudo, IdentificacaoArquivoSgq Identificacao)
{
    public bool Pdf => Path.GetExtension(Nome).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
}

public enum AcaoLoteSgq { Criar = 1, NovaRevisao = 2, AnexarNaRevisao = 3, Ignorar = 4 }

public sealed record ItemLoteSgq(string Codigo, string Titulo, string? Tipo, int Revisao, string? Editavel, string? Pdf, AcaoLoteSgq Acao, string Observacao, string? ServicoVinculado);

public sealed record ResumoLoteSgq(IReadOnlyList<ItemLoteSgq> Itens, IReadOnlyList<string> SemCodigo)
{
    public int Criar => Itens.Count(i => i.Acao == AcaoLoteSgq.Criar);
    public int Revisoes => Itens.Count(i => i.Acao is AcaoLoteSgq.NovaRevisao or AcaoLoteSgq.AnexarNaRevisao);
    public int Ignorados => Itens.Count(i => i.Acao == AcaoLoteSgq.Ignorar);
}
