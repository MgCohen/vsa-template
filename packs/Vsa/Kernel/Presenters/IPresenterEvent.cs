namespace Umpire.Vsa.Presenters;

using Umpire.Vsa.Contracts.Events;

public interface IPresenterEvent<TEvent> where TEvent : IntegrationEvent {
    Task OnEventAsync(TEvent integrationEvent, CancellationToken ct);
}
