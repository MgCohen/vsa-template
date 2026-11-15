namespace Umpire.Vsa.Handlers.Events;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class BeforeEventHook<TEvent> : IBeforeCall where TEvent : IntegrationEvent {
    public abstract Task<TEvent> BeforeAsync(IEnvelope<TEvent> envelope);

    async Task<object> IBeforeCall.CallAsync(Envelope envelope) => await BeforeAsync((IEnvelope<TEvent>)envelope);
}
