using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class IntegrationEventHandler<TEvent> : IHandler<TEvent> where TEvent : IntegrationEvent {
    protected static Task<Result<Unit>> Done { get; } = Task.FromResult<Result<Unit>>(Unit.Value);

    public abstract Task<Result<Unit>> HandleAsync(Envelope<TEvent> envelope);

    async Task<IMessageResult> IHandler.HandleAsync(Envelope envelope) =>
        await HandleAsync((Envelope<TEvent>)envelope);
}
