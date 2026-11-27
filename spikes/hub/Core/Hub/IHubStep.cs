namespace Spike.Core;

public interface IHubStep {
    Task<MessageResult> Run(object message, MessageNext next);
}
