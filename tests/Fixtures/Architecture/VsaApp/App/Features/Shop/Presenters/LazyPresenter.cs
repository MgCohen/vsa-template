namespace VsaApp.Features.Shop.Presenters;

using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;
using VsaApp.Features.Shop.Slices.Sell;

sealed class LazyPresenter(IRequestSender requests, Func<IIntegrationEventPublisher> events) : Presenter(requests) {
    public Task Finish() => events().PublishAsync(new Sold(), CancellationToken.None);
}
