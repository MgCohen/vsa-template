namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public interface IPipeline {
    IReadOnlyCollection<Type> Messages { get; }

    Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct);
}

public abstract class PipelineStep<TMessage> {
    public abstract Task<MessageResult> Run(TMessage message, MessageNext next);
}

public abstract class Validator<TMessage> {
    public abstract string? Check(TMessage message);
}

public delegate Task<object?> HandlerCall(IServiceProvider scope, object message, CancellationToken ct);

public sealed class Pipeline(IReadOnlyDictionary<Type, HandlerCall> handlers, IEnumerable<PipelineEntry> entries) : IPipeline {
    readonly Dictionary<Type, PipelineEntry[]> routes = handlers.Keys.ToDictionary(
        message => message,
        message => entries.Where(e => e.Message.IsAssignableFrom(message)).ToArray());

    public IReadOnlyCollection<Type> Messages => routes.Keys;

    public Task<MessageResult> Run(object message, IServiceProvider scope, CancellationToken ct) {
        var route = routes[message.GetType()];
        return Step(0);

        async Task<MessageResult> Step(int index) {
            if (index < route.Length) {
                return await route[index].Run(scope, message, () => Step(index + 1)).ConfigureAwait(false);
            }
            return MessageResult.Ok(await handlers[message.GetType()](scope, message, ct).ConfigureAwait(false));
        }
    }
}

public sealed record PipelineEntry(Type Message, Func<IServiceProvider, object, MessageNext, Task<MessageResult>> Run);

public static class PipelineServices {
    public static IServiceCollection AddHubStep<TStep>(this IServiceCollection services) where TStep : class, IHubStep =>
        services.AddSingleton<IHubStep, TStep>();

    public static IServiceCollection AddStep<TStep, TMessage>(this IServiceCollection services)
        where TStep : PipelineStep<TMessage> =>
        services.AddScoped<TStep>().AddSingleton(new PipelineEntry(typeof(TMessage),
            (scope, message, next) => scope.GetRequiredService<TStep>().Run((TMessage)message, next)));

    public static IServiceCollection AddValidator<TValidator, TMessage>(this IServiceCollection services)
        where TValidator : Validator<TMessage> =>
        services.AddScoped<TValidator>().AddSingleton(new PipelineEntry(typeof(TMessage),
            (scope, message, next) => scope.GetRequiredService<TValidator>().Check((TMessage)message) is { } error
                ? Task.FromResult(MessageResult.Fail(error))
                : next()));
}
