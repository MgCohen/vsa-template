using VSA.Contracts;
using VSA.Handlers;

namespace Arena.Slices.Duel;

public sealed class AuditStep(ArenaLog log) : AfterStep<ArenaMove> {
    public override Task AfterAsync(IEnvelope<ArenaMove> envelope) {
        log.Add($"audit:{envelope.Message.GetType().Name}:{envelope.State}");
        return Task.CompletedTask;
    }
}
