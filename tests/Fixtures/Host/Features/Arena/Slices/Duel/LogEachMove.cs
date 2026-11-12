using VSA.Contracts;
using VSA.Handlers;

namespace Arena.Slices.Duel;

sealed class LogEachMove(ArenaLog log) : After<ArenaMove> {
    public override Task AfterAsync(IEnvelope<ArenaMove> envelope) {
        var parent = envelope.Parent?.Message.GetType().Name ?? "none";
        log.Add($"after:{envelope.Message.GetType().Name}:{envelope.State}:{envelope.Depth}:{parent}");
        return Task.CompletedTask;
    }
}
