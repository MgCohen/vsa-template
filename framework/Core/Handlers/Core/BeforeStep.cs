using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireDerived]
[Preserve]
public abstract class BeforeStep<TMessage> : IBeforeCall where TMessage : class {
    public virtual int Order => 0;

    public abstract Task<TMessage> BeforeAsync(IEnvelope<TMessage> envelope);

    async Task<object> IBeforeCall.CallAsync(Envelope envelope) => await BeforeAsync((IEnvelope<TMessage>)envelope);
}
