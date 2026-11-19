namespace Umpire.Vsa.Handlers.Events;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

// No [AutoRegister] here. The marker is on IntegrationEventHandler<TEvent> only,
// so the module scan does not take a presenter as a module handler.
public interface IIntegrationEventHandler<TEvent> : IHandler<TEvent> where TEvent : IntegrationEvent {
    Task<Result<Unit>> HandleAsync(Envelope<TEvent> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TEvent>)envelope);
}
