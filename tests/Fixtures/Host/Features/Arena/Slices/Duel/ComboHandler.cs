using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Commands;

namespace Arena.Slices.Duel;

sealed class ComboHandler(ICommandSender commands, ArenaLog log) : CommandHandler<ComboCommand, int> {
    public override async Task<Result<int>> HandleAsync(Envelope<ComboCommand> envelope) {
        var total = 0;
        for (var hit = 0; hit < envelope.Message.Hits; hit++) {
            var struck = await commands.SendAsync(new StrikeCommand(envelope.Message.Damage), envelope.Cancellation);
            total += struck.Value;
        }
        log.Add($"handle:Combo:{total}");
        return total;
    }
}
