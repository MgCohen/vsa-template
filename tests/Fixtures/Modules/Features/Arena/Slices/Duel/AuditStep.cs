namespace Arena.Slices.Duel;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class AuditStep(ArenaLog log) : AfterStep<ArenaMove> {
    public override Task AfterAsync(IEnvelope<ArenaMove> envelope) {
        log.Add($"audit:{envelope.Message.GetType().Name}:{envelope.State}");
        return Task.CompletedTask;
    }
}
