namespace Umpire.Vsa.Presenters;

using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

public interface IPresenterHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent;
