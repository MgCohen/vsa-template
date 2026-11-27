namespace Umpire.Messages.Handlers;

using System.Collections.Concurrent;
using Umpire.Messages.Contracts;

public sealed class Pipelines {
    readonly IServiceProvider root;
    readonly IReadOnlyList<PipelineDefinition> definitions;
    readonly HubSteps hubSteps;
    readonly ConcurrentDictionary<PipelineDefinition, Pipeline> built = new();
    readonly ConcurrentDictionary<Type, IReadOnlyList<Pipeline>> broadcasts = new();
    readonly ConcurrentDictionary<Type, Pipeline> byType = new();

    internal Pipelines(IServiceProvider root, IEnumerable<PipelineDefinition> definitions, IEnumerable<HubStepEntry> hubSteps) {
        this.root = root;
        this.definitions = [.. definitions];
        this.hubSteps = new HubSteps(hubSteps);
    }

    internal Task<Result<TResult>> SendAsync<TResult>(
        Type handlerBase, object message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        return hubSteps.RunAsync(message, services, ct,
            async () => EnvelopeResults.Of<TResult>(await For(handlerBase, message.GetType()).RunAsync(message, services, ct)),
            error => (Result<TResult>)error);
    }

    internal Task<Result<Unit>> PublishAsync(
        IBroadcastMessage message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        return hubSteps.RunAsync(message, services, ct, () => RunBroadcastAsync(message, services, ct), error => (Result<Unit>)error);
    }

    internal async Task<IMessageResult> SendAsync(object message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        if (message is IBroadcastMessage broadcast) {
            return await PublishAsync(broadcast, services, ct);
        }
        return await hubSteps.RunAsync(message, services, ct,
            async () => EnvelopeResults.Untyped(await byType.GetOrAdd(message.GetType(), For).RunAsync(message, services, ct)),
            error => (IMessageResult)(Result<Unit>)error);
    }

    async Task<Result<Unit>> RunBroadcastAsync(IBroadcastMessage message, IServiceProvider services, CancellationToken ct) {
        var pipelines = broadcasts.GetOrAdd(message.GetType(), BroadcastsOf);
        if (pipelines.Count == 0) {
            throw new InvalidOperationException($"No pipeline runs {message.GetType().Name}.");
        }
        var first = await pipelines[0].RunAsync(message, services, ct);
        if (first.State == EnvelopeState.Accepted) {
            foreach (var next in pipelines.Skip(1)) {
                await next.RunAsync(message, services, ct);
            }
        }
        return EnvelopeResults.Of<Unit>(first);
    }

    static Pipeline? MostSpecific(List<Pipeline> pipelines) =>
        MessageTypes.Narrowest(pipelines, p => p.Definition.Message).LastOrDefault();

    Pipeline For(Type handlerBase, Type message) {
        var candidates = definitions
            .Where(d => d.Message.IsAssignableFrom(message) && d.Selects(handlerBase))
            .Select(Instance)
            .ToList();
        return MostSpecific([.. candidates.Where(p => p.Runs(message))])
            ?? MostSpecific(candidates)
            ?? throw new InvalidOperationException(
                $"No pipeline runs the handlers of {handlerBase.Name.Split('`')[0]}, "
                + $"so {message.Name} has no pipeline.");
    }

    Pipeline For(Type message) {
        var candidates = definitions
            .Where(d => d.Message.IsAssignableFrom(message) && !typeof(IBroadcastMessage).IsAssignableFrom(d.Message))
            .Select(Instance)
            .Where(p => p.Runs(message))
            .ToList();
        var nearest = MessageTypes.Narrowest(candidates, p => p.Definition.Message);
        return nearest.Count switch {
            1 => nearest[0],
            0 => throw new InvalidOperationException(
                $"No pipeline has a handler for {message.Name}. Add a handler for it, or send it through the sender of its kind."),
            _ => throw new InvalidOperationException(
                $"{nearest.Count} pipelines have a handler for {message.Name}, so the hub cannot select one. "
                + "Send it through the sender of its kind."),
        };
    }

    IReadOnlyList<Pipeline> BroadcastsOf(Type message) {
        var candidates = definitions
            .Where(d => typeof(IBroadcastMessage).IsAssignableFrom(d.Message) && d.Message.IsAssignableFrom(message))
            .ToList();
        return [.. candidates
            .Where(d => !candidates.Exists(o => o.SharesHandlers(d) && MessageTypes.IsNarrower(o.Message, d.Message)))
            .OrderBy(d => candidates.FindIndex(o => o.SharesHandlers(d)))
            .Select(Instance)];
    }

    Pipeline Instance(PipelineDefinition definition) => built.GetOrAdd(definition, d => new Pipeline(d, root));
}
