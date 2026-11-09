using VSA.Contracts;

namespace VSA.Handlers;

sealed class EnvelopeFactory<TMessage> : IEnvelopeFactory where TMessage : class {
    public Envelope Create(
        object message, long id, Envelope? parent, IServiceProvider services, CancellationToken ct) =>
        new Envelope<TMessage>((TMessage)message, id, parent, services, ct);
}
