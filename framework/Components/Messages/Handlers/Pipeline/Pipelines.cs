using System.Collections.Concurrent;
using VSA.Contracts;

namespace VSA.Handlers;

public sealed class Pipelines {
    readonly IServiceProvider root;
    readonly IReadOnlyList<PipelineRegistration> registrations;
    readonly ConcurrentDictionary<PipelineRegistration, IPipelineRun> built = new();

    internal Pipelines(IServiceProvider root, IEnumerable<PipelineRegistration> registrations) {
        this.root = root;
        this.registrations = [.. registrations];
    }

    public Pipeline<TMessage> Of<TMessage>() where TMessage : class {
        var registration = registrations.FirstOrDefault(r => r.HandlerBase is null && r.Message == typeof(TMessage))
            ?? throw new InvalidOperationException(
                $"No pipeline is registered for {typeof(TMessage).FullName}. "
                + $"Call services.AddPipeline(new PipelineBuilder<{typeof(TMessage).Name}>()...) in Configure.");
        return (Pipeline<TMessage>)Instance(registration);
    }

    internal async Task<Result<TResult>> SendAsync<TResult>(
        Type handlerBase, object message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        var envelope = await Instance(For(handlerBase, message.GetType())).RunAsync(message, services, ct);
        return EnvelopeResults.Of<TResult>(envelope);
    }

    PipelineRegistration For(Type handlerBase, Type message) {
        var custom = registrations
            .Where(r => r.HandlerBase is null && r.Message.IsAssignableFrom(message) && Instance(r).Runs(message))
            .ToList();
        return custom.FirstOrDefault(c => custom.TrueForAll(o => o.Message.IsAssignableFrom(c.Message)))
            ?? registrations.FirstOrDefault(r => r.HandlerBase == handlerBase)
            ?? throw new InvalidOperationException(
                $"No pipeline runs the handlers of {handlerBase.Name.Split('`')[0]}, "
                + $"so {message.Name} has no pipeline.");
    }

    IPipelineRun Instance(PipelineRegistration registration) =>
        built.GetOrAdd(registration, r => r.Build(root));
}
