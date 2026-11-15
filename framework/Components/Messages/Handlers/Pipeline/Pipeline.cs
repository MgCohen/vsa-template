using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

public sealed class Pipeline<TMessage> : IPipelineRun where TMessage : class {
    readonly PipelineDefinition definition;
    readonly IServiceProvider services;
    readonly PipelineCatalog catalog;
    readonly bool many = typeof(IBroadcastMessage).IsAssignableFrom(typeof(TMessage));
    readonly Dictionary<Type, List<Type>> handlers;
    readonly ConcurrentDictionary<Type, MessageRoute> routes = new();

    internal Pipeline(PipelineDefinition definition, IServiceProvider services) {
        this.definition = definition;
        this.services = services;
        catalog = services.GetService<PipelineCatalog>() ?? PipelineCatalog.Empty;
        handlers = HandlerIndex();
        Runner = (IRunner)ActivatorUtilities.CreateInstance(services, definition.Runner);
    }

    public IRunner Runner { get; }

    public async Task<Result<TResult>> SendAsync<TResult>(TMessage message, CancellationToken ct = default) =>
        EnvelopeResults.Of<TResult>(await RunAsync(message, services, ct));

    public Task StartAsync() => Runner.StartAsync();

    bool IPipelineRun.Runs(Type message) => many || handlers.ContainsKey(message);

    Task<Envelope> IPipelineRun.RunAsync(object message, IServiceProvider scope, CancellationToken ct) =>
        RunAsync(message, scope, ct);

    static Type[] ByName(IEnumerable<StageType> types, Type message) =>
        [.. types.Where(t => t.Message.IsAssignableFrom(message))
            .Select(t => t.Implementation)
            .Distinct()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)];

    static Type[] Steps(IReadOnlyList<Type> steps, Type typedStepBase, Type message) =>
        [.. steps.Where(step => MessageTypes.OfStep(step, typedStepBase).IsAssignableFrom(message))];

    async Task<Envelope> RunAsync(object message, IServiceProvider scope, CancellationToken ct) {
        if (message is not TMessage) {
            throw new ArgumentException(
                $"{message?.GetType().FullName ?? "null"} is not a {typeof(TMessage).Name}, "
                + "so this pipeline cannot run it.",
                nameof(message));
        }
        var route = routes.GetOrAdd(message.GetType(), Route);
        var envelope = route.Create(message, CurrentEnvelope.Value, scope, ct);
        await Runner.RunAsync(envelope, e => PipelineRun.ExecuteAsync(route, e));
        return envelope;
    }

    Dictionary<Type, List<Type>> HandlerIndex() {
        var index = new Dictionary<Type, List<Type>>();
        var all = definition.HandlerBases.SelectMany(catalog.HandlersOf).Concat(definition.Handlers).Distinct();
        foreach (var handler in all) {
            var message = MessageTypes.OfHandler(handler);
            if (!index.TryGetValue(message, out var list)) {
                index[message] = list = [];
            }
            list.Add(handler);
            if (!many && list.Count > 1) {
                throw new InvalidOperationException(
                    $"{message.FullName} has two handlers: {list[0].FullName} and {list[1].FullName}. "
                    + "A command or a request has one handler.");
            }
        }
        return index;
    }

    MessageRoute Route(Type message) {
        var hooks = definition.HookBases.SelectMany(catalog.HooksOf).ToList();
        return new MessageRoute(message, handlers.TryGetValue(message, out var found) ? found : [], many) {
            Validators = ByName(catalog.Validators, message),
            BeforeSteps = Steps(definition.BeforeSteps, typeof(BeforeStep<>), message),
            BeforeHooks = ByName(hooks.Where(h => typeof(IBeforeCall).IsAssignableFrom(h.Implementation)), message),
            AfterHooks = ByName(hooks.Where(h => typeof(IAfterCall).IsAssignableFrom(h.Implementation)), message),
            AfterSteps = Steps(definition.AfterSteps, typeof(AfterStep<>), message),
        };
    }
}
