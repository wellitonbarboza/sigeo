using Sigeo.Domain.Suprimentos;

namespace Sigeo.Application.Suprimentos;

/// <summary>Transferências da lista (com itens) e o número do recebimento de origem de cada uma.</summary>
public sealed record TransferenciasLista(List<TransferenciaMaterial> Transferencias, Dictionary<Guid, int> NumeroRecebimento);
