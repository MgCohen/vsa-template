namespace Cards.Slices.PayCost;

using Resources.Domain.Shared;
using Resources.Slices.ChangeResource;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Commands;

sealed class PayCostHandler(ICommandSender commands, IIntegrationEventPublisher events)
    : CommandHandler<PayCostCommand, PayCostResult> {
    public override async Task<Result<PayCostResult>> HandleAsync(IEnvelope<PayCostCommand> envelope) {
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
