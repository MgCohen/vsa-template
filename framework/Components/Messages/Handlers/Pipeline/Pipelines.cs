namespace Umpire.Messages.Handlers;

using System.Collections.Concurrent;
using Umpire.Messages.Contracts;

public sealed class Pipelines {
    readonly IServiceProvider root;
    readonly IReadOnlyList<PipelineDefinition> definitions;
    readonly ConcurrentDictionary<PipelineDefinition, Pipeline> built = new();
    readonly ConcurrentDictionary<Type, IReadOnlyList<Pipeline>> broadcasts = new();

    internal Pipelines(IServiceProvider root, IEnumerable<PipelineDefinition> definitions) {
        this.root = root;
        this.definitions = [.. definitions];
    }

    internal async Task<Result<TResult>> SendAsync<TResult>(
        Type handlerBase, object message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
        var envelope = await For(handlerBase, message.GetType()).RunAsync(message, services, ct);
        return EnvelopeResults.Of<TResult>(envelope);
    }

    internal async Task<Result<Unit>> PublishAsync(
        IBroadcastMessage message, IServiceProvider services, CancellationToken ct) {
        if (message is null) {
            throw new ArgumentNullException(nameof(message));
        }
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
