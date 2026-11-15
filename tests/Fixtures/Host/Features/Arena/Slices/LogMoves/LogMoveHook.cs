using Arena.Slices.Duel;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Arena.Slices.LogMoves;

sealed class LogMoveHook(ArenaLog log) : AfterCommandHook<ArenaCommand<int>, int> {
    public override Task AfterAsync(IEnvelope<ArenaCommand<int>> envelope, Result<int> outcome) {
        var parent = envelope.Parent?.Message.GetType().Name ?? "none";
        log.Add($"after:{envelope.Message.GetType().Name}:{envelope.State}:{envelope.Depth}:{parent}");
        return Task.CompletedTask;
    }
}
