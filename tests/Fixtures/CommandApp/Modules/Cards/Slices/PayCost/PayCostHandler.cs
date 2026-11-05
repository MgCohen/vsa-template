using Resources.Slices.ChangeResource;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Handlers.Commands;

namespace Cards.Slices.PayCost;

// A command handler that sends a command of another module, and publishes an event of its own module.
sealed class PayCostHandler(ICommandSender commands, IIntegrationEventPublisher events) : CommandHandler<PayCostCommand, PayCostResult> {
    public override async Task<PayCostResult> HandleAsync(PayCostCommand command, CancellationToken ct) {
        var changed = await commands.SendAsync(new ChangeResourceCommand(command.Cost.Resource, -command.Cost.Amount), ct);
        if (changed.Changed) {
            await events.PublishAsync(new CostPaid(command.Cost), ct);
        }
        return new PayCostResult(changed.Changed, changed.Value);
    }
}
