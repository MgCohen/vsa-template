namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public abstract class AfterStep : IAfterCall {
    public abstract Task AfterAsync(Envelope envelope);

    Task IAfterCall.CallAsync(Envelope envelope) => AfterAsync(envelope);
}

public abstract class AfterStep<TMessage> : AfterStep where TMessage : class {
    public abstract Task AfterAsync(IEnvelope<TMessage> envelope);

    public sealed override Task AfterAsync(Envelope envelope) => AfterAsync((IEnvelope<TMessage>)envelope);
}
