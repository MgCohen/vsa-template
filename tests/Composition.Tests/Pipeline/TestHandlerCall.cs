namespace Composition.Tests.Pipeline;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class TestHandlerCall<TMessage> : HandlerCall<ITestHandler<TMessage>, TMessage> where TMessage : class {
    public override Task<IMessageResult> CallAsync(ITestHandler<TMessage> handler, IEnvelope<TMessage> envelope) =>
        handler.RunAsync(envelope);
}
