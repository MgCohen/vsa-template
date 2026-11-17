namespace Umpire.Messages.Handlers;

using System.Collections.Concurrent;
using Umpire.Messages.Contracts;

public sealed class Pipelines {
    readonly IServiceProvider root;
    readonly IReadOnlyList<PipelineDefinition> definitions;
    readonly ConcurrentDictionary<PipelineDefinition, Pipeline> built = new();

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

    static Pipeline? MostSpecific(List<Pipeline> pipelines) =>
        pipelines.LastOrDefault(p => !pipelines.Exists(o => IsNarrower(o.Definition.Message, p.Definition.Message)));

    static bool IsNarrower(Type message, Type than) => message != than && than.IsAssignableFrom(message);

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

    Pipeline Instance(PipelineDefinition definition) => built.GetOrAdd(definition, d => new Pipeline(d, root));
}
