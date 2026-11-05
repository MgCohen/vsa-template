using VSA.Handlers.Events;

namespace Signals.Slices.Ring;

sealed class RangHandler(RingLog log) : IntegrationEventHandler<Rang> {
    public override Task HandleAsync(Rang integrationEvent, CancellationToken ct) {
        log.Add(integrationEvent.Bell);
        return Task.CompletedTask;
    }
}
