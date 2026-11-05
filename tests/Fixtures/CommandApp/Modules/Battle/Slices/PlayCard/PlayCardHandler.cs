using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Resources.Domain.Shared;
using VSA.Contracts.Commands;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Battle.Slices.PlayCard;

// A request handler that sends a command of Cards, with the published domain of Cards and of Resources.
sealed class PlayCardHandler(ICommandSender commands) : RoutedHandler<PlayCardRequest, PlayCardResponse> {
    protected override async Task<Result<PlayCardResponse>> HandleRequestAsync(PlayCardRequest request, CancellationToken ct) {
        var paid = await commands.SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, request.Mana)), ct);
        return new PlayCardResponse(paid.Paid, paid.Left);
    }
}
