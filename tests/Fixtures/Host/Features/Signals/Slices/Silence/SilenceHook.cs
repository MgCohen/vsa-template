namespace Signals.Slices.Silence;

using Signals.Slices.Ring;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;

sealed class SilenceHook : BeforeEventHook<Rang> {
    public const string Code = "signals.silent";

    public override Task<Rang> BeforeAsync(IEnvelope<Rang> envelope) {
        if (envelope.Message.Bell == "silent") {
            envelope.Reject(new Error(Code, "A silent bell does not ring."));
        }
        return Task.FromResult(envelope.Message);
    }
}
