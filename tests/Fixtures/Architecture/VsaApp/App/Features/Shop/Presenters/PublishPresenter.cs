namespace VsaApp.Features.Shop.Presenters;

using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;
using VsaApp.Features.Shop.Slices.Sell;

sealed class PublishPresenter(IRequestSender requests, Func<object> service) : Presenter(requests) {
    public async Task Finish() => await ((IIntegrationEventPublisher)service()).PublishAsync(new Sold(), CancellationToken.None);
}
