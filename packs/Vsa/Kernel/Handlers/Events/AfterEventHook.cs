namespace Umpire.Vsa.Handlers.Events;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class AfterEventHook<TEvent> : IAfterCall where TEvent : IntegrationEvent {
    public abstract Task AfterAsync(IEnvelope<TEvent> envelope);

    Task IAfterCall.CallAsync(Envelope envelope) => AfterAsync((IEnvelope<TEvent>)envelope);
}
