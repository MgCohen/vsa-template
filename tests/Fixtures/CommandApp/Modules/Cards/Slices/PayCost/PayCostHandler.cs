using Resources.Domain.Shared;
using Resources.Slices.ChangeResource;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Handlers.Commands;

namespace Cards.Slices.PayCost;

// A command handler that sends a command of another module, and publishes an event of its own module.
sealed class PayCostHandler(ICommandSender commands, IIntegrationEventPublisher events)
    : CommandHandler<PayCostCommand, PayCostResult> {
    public override async Task<Result<PayCostResult>> HandleAsync(Envelope<PayCostCommand> envelope) {
        var cost = envelope.Message.Cost;
        var changed = await commands.SendAsync(new ChangeResourceCommand(new ResourceKey(cost.Resource), -cost.Amount), envelope.Cancellation);
        if (!changed.IsSuccess) {
            return changed.Error!;
        }
        if (changed.Value.Changed) {
            await events.PublishAsync(new CostPaid(cost), envelope.Cancellation);
        }
        return new PayCostResult(changed.Value.Changed, changed.Value.Value);
    }
}
