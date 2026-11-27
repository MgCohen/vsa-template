namespace Spike.Core;

public interface IPipelineStep {
    Type Message { get; }

    Task<MessageResult> Run(object message, MessageNext next);
}
