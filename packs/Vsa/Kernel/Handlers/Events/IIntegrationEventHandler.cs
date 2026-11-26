namespace Umpire.Vsa.Handlers.Events;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

public interface IIntegrationEventHandler<TEvent> : IHandler<TEvent> where TEvent : IntegrationEvent {
    Task<Result<Unit>> HandleAsync(IEnvelope<TEvent> envelope);
}
