using Arena.Slices.Duel;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Commands;

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
