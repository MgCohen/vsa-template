namespace VsaApp.Features.Shop.Slices.LogSale;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Events;
using VsaApp.Features.Shop.Slices.Sell;

sealed class LazySellHandler(Lazy<IRequestSender> requests) : IntegrationEventHandler<Sold> {
    public override Task<Result<Unit>> HandleAsync(IEnvelope<Sold> envelope) =>
        Task.FromResult<Result<Unit>>(requests.IsValueCreated ? Unit.Value : new Error("shop.none", "None."));
}
