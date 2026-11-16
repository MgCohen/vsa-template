namespace Signals.Slices.Chime;

using Signals.Slices.Ring;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;

sealed class ChimeHook(RingLog log) : AfterEventHook<Rang> {
    public override Task AfterAsync(IEnvelope<Rang> envelope) {
        log.AddHook($"chime:{envelope.Message.Bell}:{envelope.State}");
        return Task.CompletedTask;
    }
}
