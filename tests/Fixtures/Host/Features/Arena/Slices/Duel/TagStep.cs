using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace Arena.Slices.Duel;

public sealed class TagStep : BeforeStep {
    public override Task<object> BeforeAsync(Envelope envelope) {
        envelope.Items["tag"] = "arena";
        return Task.FromResult(envelope.Message);
    }
}
