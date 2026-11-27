namespace Spike.Core;

public interface IPipelineStep {
    Task<MessageResult> Run(object message, MessageNext next);
}
