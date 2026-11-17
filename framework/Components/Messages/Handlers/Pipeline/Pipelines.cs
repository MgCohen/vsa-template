namespace Umpire.Messages.Handlers;

using Umpire.Messages.Contracts;

public sealed class Pipelines {
    readonly Lazy<IReadOnlyList<Pipeline>> all;

    internal Pipelines(IServiceProvider root, IEnumerable<PipelineDefinition> definitions) {
        all = new(() => [.. definitions.Select(d => new Pipeline(d, root))]);
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
        var candidates = all.Value
            .Where(p => p.Definition.Message.IsAssignableFrom(message) && p.Selects(handlerBase))
            .ToList();
        return MostSpecific([.. candidates.Where(p => p.Runs(message))])
            ?? MostSpecific(candidates)
            ?? throw new InvalidOperationException(
                $"No pipeline runs the handlers of {handlerBase.Name.Split('`')[0]}, "
                + $"so {message.Name} has no pipeline.");
    }
}
