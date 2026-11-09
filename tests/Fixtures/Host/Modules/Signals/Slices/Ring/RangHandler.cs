using VSA.Contracts;
using VSA.Handlers.Events;

namespace Signals.Slices.Ring;

sealed class RangHandler(RingLog log) : IntegrationEventHandler<Rang> {
    public override Task<Result<Unit>> HandleAsync(Envelope<Rang> envelope) {
        log.Add(envelope.Message.Bell);
        return Done;
    }
}
