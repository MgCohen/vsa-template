namespace Spike.Core;

public interface IPipeline {
    IReadOnlyCollection<Type> Messages { get; }

    Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct);
}
