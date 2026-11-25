namespace VsaApp.Features.Shop.Slices.Sell;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Requests;

sealed class SellHandler(IIntegrationEventPublisher events) : RequestHandler<SellRequest, SellResponse> {
    public override async Task<Result<SellResponse>> HandleAsync(IEnvelope<SellRequest> envelope) {
        await events.PublishAsync(new Sold(), CancellationToken.None);
        return new SellResponse();
    }
}
