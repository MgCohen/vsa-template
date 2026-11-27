namespace Spike.Core;

public interface IPipeline {
    bool Runs(Type message);

    Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct);
}
