using Sigeo.Domain.Sst;

namespace Sigeo.Application.Sst;

/// <summary>Nome e empresa (fornecedor) do colaborador.</summary>
public sealed record ColaboradorResumo(Guid Id, string Nome, Guid? FornecedorId);

public sealed record DocumentosColaboradorDados(Dictionary<Guid, string> Fornecedores, Dictionary<Guid, ColaboradorResumo> Colaboradores, List<DocumentoColaborador> Documentos);
