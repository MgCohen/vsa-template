using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireDerived]
[Preserve]
public abstract class AfterStep<TMessage> : IAfterCall where TMessage : class {
    public virtual int Order => 0;

    public abstract Task AfterAsync(IEnvelope<TMessage> envelope);

    Task IAfterCall.CallAsync(Envelope envelope) => AfterAsync((IEnvelope<TMessage>)envelope);
}
