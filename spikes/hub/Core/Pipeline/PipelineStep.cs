namespace Spike.Core;

public abstract class PipelineStep<TMessage> : IPipelineStep where TMessage : class {
    public Type Message => typeof(TMessage);

    public Task<MessageResult> Run(object message, MessageNext next) => Run((TMessage)message, next);

    protected abstract Task<MessageResult> Run(TMessage message, MessageNext next);
}
