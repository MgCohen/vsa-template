using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers;

namespace Arena.Slices.Duel;

sealed class Counter(ICommandSender commands, ArenaLog log) : After<StrikeCommand> {
    public override int Order => 10;

    public override Task AfterAsync(IEnvelope<StrikeCommand> envelope) {
        if (envelope.State == EnvelopeState.Accepted && envelope.Message.Damage >= 10) {
            log.Add("counter");
            _ = commands.SendAsync(new StrikeCommand(1), envelope.Cancellation);
        }
        return Task.CompletedTask;
    }
}
