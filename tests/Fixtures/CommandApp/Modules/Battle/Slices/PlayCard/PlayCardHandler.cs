using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Resources.Domain.Shared;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Requests;

namespace Battle.Slices.PlayCard;

// A request handler that sends a command of Cards, with the published domain of Cards and of Resources.
sealed class PlayCardHandler(ICommandSender commands) : RequestHandler<PlayCardRequest, PlayCardResponse> {
    public override async Task<Result<PlayCardResponse>> HandleAsync(Envelope<PlayCardRequest> envelope) {
        var cost = new Cost(ResourceKeys.Mana, envelope.Message.Mana);
        var paid = await commands.SendAsync(new PayCostCommand(cost), envelope.Cancellation);
        return paid.IsSuccess ? new PlayCardResponse(paid.Value.Paid, paid.Value.Left) : paid.Error!;
    }
}
