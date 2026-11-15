using Arena.Slices.Duel;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;

namespace Arena.Slices.Counter;

sealed class CounterHook(ICommandSender commands, ArenaLog log) : AfterCommandHook<StrikeCommand, int> {
    public override Task AfterAsync(IEnvelope<StrikeCommand> envelope, Result<int> outcome) {
        if (outcome.IsSuccess && outcome.Value >= 10) {
            log.Add("counter");
            _ = commands.SendAsync(new StrikeCommand(1), envelope.Cancellation);
        }
        return Task.CompletedTask;
    }
}
