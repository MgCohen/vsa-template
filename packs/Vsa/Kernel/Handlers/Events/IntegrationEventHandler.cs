namespace Umpire.Vsa.Handlers.Events;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

[AutoRegister]
public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler<TEvent>, IRunnableHandler<TEvent>
    where TEvent : IntegrationEvent {
    protected static Task<Result<Unit>> Done { get; } = Task.FromResult<Result<Unit>>(Unit.Value);

    public abstract Task<Result<Unit>> HandleAsync(IEnvelope<TEvent> envelope);

    async Task<IMessageResult> IRunnableHandler<TEvent>.RunAsync(IEnvelope<TEvent> envelope) =>
        await HandleAsync(envelope);
}
