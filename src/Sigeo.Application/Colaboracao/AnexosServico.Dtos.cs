using Microsoft.EntityFrameworkCore;
using Sigeo.Domain.Common;

namespace Sigeo.Application.Colaboracao;

/// <summary>Arquivo vindo da tela, sem depender do tipo do Blazor (o serviço só precisa abrir o conteúdo com um teto de bytes).</summary>
public sealed record ArquivoEnviado(string Nome, long Tamanho, string? ContentType, Func<long, Stream> Abrir);
