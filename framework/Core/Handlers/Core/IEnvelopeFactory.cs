using VSA.Contracts;

namespace VSA.Handlers;

interface IEnvelopeFactory {
    Envelope Create(object message, long id, Envelope? parent, IServiceProvider services, CancellationToken ct);
}
