namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public sealed record MessageResult(object? Value, string? Error) {
    public static MessageResult Ok(object? value) => new(value, null);

    public static MessageResult Fail(string error) => new(null, error);
}

public delegate Task<MessageResult> MessageNext();

public interface IHubStep {
    Task<MessageResult> Run(object message, MessageNext next);
}

public interface IHub {
    Task<MessageResult> Send(object message, CancellationToken ct = default);
}

public sealed class Hub(IEnumerable<IHubStep> steps, IEnumerable<IPipeline> pipelines) {
    readonly IHubStep[] hubSteps = [.. steps];
    readonly Dictionary<Type, IPipeline> byMessage = pipelines
        .SelectMany(p => p.Messages.Select(m => KeyValuePair.Create(m, p)))
        .ToDictionary();

    internal Task<MessageResult> Send(object message, IServiceProvider scope, CancellationToken ct) {
        if (!byMessage.TryGetValue(message.GetType(), out var pipeline)) {
            return Task.FromResult(MessageResult.Fail($"No pipeline runs {message.GetType().Name}."));
        }
        return Step(0);

        Task<MessageResult> Step(int index) => index < hubSteps.Length
            ? hubSteps[index].Run(message, () => Step(index + 1))
            : pipeline.Run(message, scope, ct);
    }
}

sealed class ScopedHub(Hub hub, IServiceProvider scope) : IHub {
    public Task<MessageResult> Send(object message, CancellationToken ct = default) => hub.Send(message, scope, ct);
}
