namespace Arena.Slices.Finisher;

using Arena.Slices.Duel;
using Umpire.Messages.Contracts;

sealed class UppercutHandler(ArenaLog log) : FinisherHandler<UppercutCommand> {
    public override Task<Result<int>> HandleAsync(IEnvelope<UppercutCommand> envelope) {
        log.Add("handle:Uppercut");
        return Task.FromResult<Result<int>>(9);
    }
}
