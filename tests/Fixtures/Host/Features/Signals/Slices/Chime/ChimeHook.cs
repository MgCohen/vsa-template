using Signals.Slices.Ring;
using VSA.Contracts;
using VSA.Handlers.Events;

namespace Signals.Slices.Chime;

sealed class ChimeHook(RingLog log) : AfterEventHook<Rang> {
    public override Task AfterAsync(IEnvelope<Rang> envelope) {
        log.AddHook($"chime:{envelope.Message.Bell}:{envelope.State}");
        return Task.CompletedTask;
    }
}
