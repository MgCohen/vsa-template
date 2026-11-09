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

    public IPipeline<TMessage> Of<TMessage>() where TMessage : class {
        var registration = registrations.FirstOrDefault(r => r.Kind is null && r.Message == typeof(TMessage))
            ?? throw new InvalidOperationException(
                $"No pipeline is registered for {typeof(TMessage).FullName}. "
                + $"Call services.AddPipeline(new PipelineBuilder<{typeof(TMessage).Name}>()...) in Configure.");
        return (IPipeline<TMessage>)Instance(registration);
    }

    internal async Task<Result<TResult>> SendAsync<TResult>(
        PipelineKind kind, object message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        var envelope = await Instance(For(kind, message.GetType())).RunAsync(message, services, ct);
        return EnvelopeResults.Of<TResult>(envelope);
    }

    PipelineRegistration For(PipelineKind kind, Type message) {
        var custom = registrations.Where(r => r.Kind is null && r.Message.IsAssignableFrom(message)).ToList();
        return custom.FirstOrDefault(c => custom.TrueForAll(o => o.Message.IsAssignableFrom(c.Message)))
            ?? registrations.FirstOrDefault(r => r.Kind == kind)
            ?? throw new InvalidOperationException($"No pipeline runs the {kind.ToString().ToLowerInvariant()}.");
    }

    IPipelineRun Instance(PipelineRegistration registration) =>
        built.GetOrAdd(registration, r => r.Build(root));
}
