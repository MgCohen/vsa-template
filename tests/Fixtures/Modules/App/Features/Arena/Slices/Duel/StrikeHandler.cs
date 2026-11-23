namespace Arena.Slices.Duel;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class StrikeHandler(ArenaLog log) : CommandHandler<StrikeCommand, int> {
    public override Task<Result<int>> HandleAsync(IEnvelope<StrikeCommand> envelope) {
        var tag = envelope.Items.TryGetValue("tag", out var value) ? value : "none";
        log.Add($"handle:Strike:{envelope.Message.Damage}:{tag}");
        return Task.FromResult<Result<int>>(envelope.Message.Damage);
    }
}
