namespace Umpire.Vsa.Handlers.Events;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

[AutoRegister]
public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
    protected static Task<Result<Unit>> Done { get; } = Task.FromResult<Result<Unit>>(Unit.Value);

    public abstract Task<Result<Unit>> HandleAsync(IEnvelope<TEvent> envelope);

    async Task<IMessageResult> Umpire.Messages.Handlers.IHandler<TEvent>.RunAsync(IEnvelope<TEvent> envelope) =>
        await HandleAsync(envelope);
}
