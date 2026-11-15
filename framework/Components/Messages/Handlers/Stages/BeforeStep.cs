namespace Umpire.Messages.Handlers;

using Umpire.Markers;
using Umpire.Messages.Contracts;

[RequireDerived]
[Preserve]
public abstract class BeforeStep : IBeforeCall {
    public abstract Task<object> BeforeAsync(Envelope envelope);

    Task<object> IBeforeCall.CallAsync(Envelope envelope) => BeforeAsync(envelope);
}

[RequireDerived]
[Preserve]
public abstract class BeforeStep<TMessage> : BeforeStep where TMessage : class {
    public abstract Task<TMessage> BeforeAsync(IEnvelope<TMessage> envelope);

    public sealed override async Task<object> BeforeAsync(Envelope envelope) =>
        await BeforeAsync((IEnvelope<TMessage>)envelope);
}
