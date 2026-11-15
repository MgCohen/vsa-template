namespace Arena.Slices.Duel;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class TagStep : BeforeStep {
    public override Task<object> BeforeAsync(Envelope envelope) {
        envelope.Items["tag"] = "arena";
        return Task.FromResult(envelope.Message);
    }
}
