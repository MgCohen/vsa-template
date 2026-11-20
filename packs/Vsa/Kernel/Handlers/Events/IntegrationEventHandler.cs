namespace Umpire.Vsa.Handlers.Events;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class IntegrationEventHandler<TEvent> : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
    protected static Task<Result<Unit>> Done { get; } = Task.FromResult<Result<Unit>>(Unit.Value);

    public abstract Task<Result<Unit>> HandleAsync(IEnvelope<TEvent> envelope);
}
