using VSA.Contracts;
using VSA.Handlers;

namespace Arena.Slices.Duel;

public sealed class TagStep : BeforeStep<ArenaMove> {
    public override Task<ArenaMove> BeforeAsync(IEnvelope<ArenaMove> envelope) {
        envelope.Items["tag"] = "arena";
        return Task.FromResult(envelope.Message);
    }
}
