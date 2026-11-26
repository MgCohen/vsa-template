namespace Composition.Tests.Pipeline;

using Umpire.Messages.Contracts;

public interface ITestHandler<in TMessage> : IHandler<TMessage> where TMessage : class {
    Task<IMessageResult> RunAsync(IEnvelope<TMessage> envelope);
}
