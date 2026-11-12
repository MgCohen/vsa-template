using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireImplementors]
interface IEnvelopeFactory {
    Envelope Create(object message, long id, Envelope? parent, IServiceProvider services, CancellationToken ct);
}
