namespace Battle.Slices.PlayCard;

using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Resources.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;

// A request handler that sends a command of Cards, with the published domain of Cards and of Resources.
sealed class PlayCardHandler(ICommandSender commands) : RequestHandler<PlayCardRequest, PlayCardResponse> {
    public override async Task<Result<PlayCardResponse>> HandleAsync(Envelope<PlayCardRequest> envelope) {
        var cost = new Cost(ResourceKeys.Mana.Name, envelope.Message.Mana);
        var paid = await commands.SendAsync(new PayCostCommand(cost), envelope.Cancellation);
        return paid.IsSuccess ? new PlayCardResponse(paid.Value.Paid, paid.Value.Left) : paid.Error!;
    }
}
