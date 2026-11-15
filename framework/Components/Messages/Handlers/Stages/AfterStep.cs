using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

[RequireDerived]
[Preserve]
public abstract class AfterStep : IAfterCall {
    public abstract Task AfterAsync(Envelope envelope);

    Task IAfterCall.CallAsync(Envelope envelope) => AfterAsync(envelope);
}

[RequireDerived]
[Preserve]
public abstract class AfterStep<TMessage> : AfterStep where TMessage : class {
    public abstract Task AfterAsync(IEnvelope<TMessage> envelope);

    public sealed override Task AfterAsync(Envelope envelope) => AfterAsync((IEnvelope<TMessage>)envelope);
}
