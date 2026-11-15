using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class AfterEventHook<TEvent> : IAfterCall where TEvent : IntegrationEvent {
    public abstract Task AfterAsync(IEnvelope<TEvent> envelope);

    Task IAfterCall.CallAsync(Envelope envelope) => AfterAsync((IEnvelope<TEvent>)envelope);
}
