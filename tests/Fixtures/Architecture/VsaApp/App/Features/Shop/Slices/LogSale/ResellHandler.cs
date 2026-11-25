namespace VsaApp.Features.Shop.Slices.LogSale;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Events;
using VsaApp.Features.Shop.Slices.Sell;

sealed class ResellHandler(Func<object> service) : IntegrationEventHandler<Sold> {
    public override async Task<Result<Unit>> HandleAsync(IEnvelope<Sold> envelope) {
        await Task.Run(() => ((IRequestSender)service()).SendAsync(new SellRequest()));
        return Unit.Value;
    }
}
