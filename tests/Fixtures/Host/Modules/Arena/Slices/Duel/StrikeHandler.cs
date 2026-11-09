using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Arena.Slices.Duel;

sealed class StrikeHandler(ArenaLog log) : CommandHandler<StrikeCommand, int> {
    public override Task<Result<int>> HandleAsync(Envelope<StrikeCommand> envelope) {
        var tag = envelope.Items.TryGetValue("tag", out var value) ? value : "none";
        log.Add($"handle:Strike:{envelope.Message.Damage}:{tag}");
        return Task.FromResult<Result<int>>(envelope.Message.Damage);
    }
}
