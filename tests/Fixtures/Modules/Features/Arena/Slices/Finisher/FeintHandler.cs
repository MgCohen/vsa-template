namespace Arena.Slices.Finisher;

using Arena.Slices.Duel;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FeintHandler(ArenaLog log) : CommandHandler<FeintCommand, int> {
    public override Task<Result<int>> HandleAsync(Envelope<FeintCommand> envelope) {
        log.Add("handle:Feint");
        return Task.FromResult<Result<int>>(1);
    }
}
