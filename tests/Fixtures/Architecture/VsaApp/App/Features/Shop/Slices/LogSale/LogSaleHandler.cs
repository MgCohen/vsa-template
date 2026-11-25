namespace VsaApp.Features.Shop.Slices.LogSale;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;
using VsaApp.Features.Shop.Slices.Sell;

sealed class LogSaleHandler : IntegrationEventHandler<Sold> {
    public override Task<Result<Unit>> HandleAsync(IEnvelope<Sold> envelope) => Task.FromResult<Result<Unit>>(Unit.Value);
}
