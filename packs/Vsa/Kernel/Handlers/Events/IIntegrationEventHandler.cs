namespace Umpire.Vsa.Handlers.Events;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

public interface IIntegrationEventHandler<TEvent> : IHandler<TEvent> where TEvent : IntegrationEvent {
    Task<Result<Unit>> HandleAsync(Envelope<TEvent> envelope);

    async Task<IMessageResult> IHandler<TEvent>.RunAsync(Envelope<TEvent> envelope) =>
        await HandleAsync(envelope);
}
