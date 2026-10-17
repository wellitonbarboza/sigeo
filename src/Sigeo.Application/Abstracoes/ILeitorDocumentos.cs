namespace Sigeo.Application.Abstracoes;

/// <summary>Leitura de PDF/imagem (laudo, certificado) em texto — Azure AI Document Intelligence (modelo de layout).</summary>
public interface ILeitorDocumentos
{
    bool Configurado { get; }
    Task<(string Texto, int Paginas)> ExtrairTextoAsync(Stream arquivo, string contentType, CancellationToken ct = default);
}
