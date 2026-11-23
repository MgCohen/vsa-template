namespace Signals.Slices.Ring;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;

sealed class RangHandler(RingLog log) : IntegrationEventHandler<Rang> {
    public override Task<Result<Unit>> HandleAsync(IEnvelope<Rang> envelope) {
        log.Add(envelope.Message.Bell);
        return Done;
    }
}
