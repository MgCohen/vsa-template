namespace Umpire.Messages.Handlers;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

public sealed class Pipeline {
    readonly IServiceProvider services;
    readonly IRunner runner;
    readonly AppTypes? types;
    readonly bool many;
    readonly Dictionary<Type, List<Type>> handlers;
    readonly ConcurrentDictionary<Type, MessageRoute> routes = new();

    internal Pipeline(PipelineDefinition definition, IServiceProvider services) {
        Definition = definition;
        this.services = services;
        types = services.GetService<AppTypes>();
        many = typeof(IBroadcastMessage).IsAssignableFrom(definition.Message);
        handlers = HandlerIndex();
        runner = CreateRunner(definition, services);
    }

    internal PipelineDefinition Definition { get; }

    public async Task<Result<TResult>> SendAsync<TResult>(object message, CancellationToken ct = default) =>
        EnvelopeResults.Of<TResult>(await RunAsync(message, services, ct));

    internal bool Runs(Type message) => many || handlers.ContainsKey(message);

    internal async Task<Envelope> RunAsync(object message, IServiceProvider scope, CancellationToken ct) {
        if (!Definition.Message.IsInstanceOfType(message)) {
            throw new ArgumentException(
                $"{message?.GetType().FullName ?? "null"} is not a {Definition.Message.Name}, "
                + "so this pipeline cannot run it.",
                nameof(message));
        }
        var route = routes.GetOrAdd(message.GetType(), Route);
        var envelope = route.Create(message, CurrentEnvelope.Value, scope, ct);
        await runner.RunAsync(envelope, e => PipelineRun.ExecuteAsync(route, e));
        return envelope;
    }

    static IRunner CreateRunner(PipelineDefinition definition, IServiceProvider services) {
        try {
            return (IRunner)ActivatorUtilities.CreateInstance(services, definition.Runner);
        } catch (InvalidOperationException e) {
            throw new InvalidOperationException(
                $"The pipeline of {definition.Message.Name} cannot create its runner {definition.Runner.FullName}: "
                + $"{e.Message} Register each service of the runner constructor, "
                + "or give the runner a public constructor.", e);
        }
    }

    static Type[] ByName(IEnumerable<ServiceImplementation> found, Type message) =>
        [.. found.Where(t => t.Service.GetGenericArguments()[0].IsAssignableFrom(message))
            .Select(t => t.Implementation)
            .Distinct()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)];

    static Type[] Steps(IReadOnlyList<Type> steps, Type typedStepBase, Type message) =>
        [.. steps.Where(step => MessageTypes.OfStep(step, typedStepBase).IsAssignableFrom(message))];

    IReadOnlyList<ServiceImplementation> Found(Type genericBase) =>
        types?.Implementations(genericBase) ?? [];

    IEnumerable<Type> HandlersOf(Type handlerBase) =>
        types is null
            ? throw new InvalidOperationException(
                $"HandlersOf({handlerBase.Name}) needs the module types, and these services have none. "
                + "Build the pipeline from the services of an Umpire app, "
                + "or list the handlers with Handlers(typeof(...)).")
            : types.Implementations(handlerBase).Select(h => h.Implementation);

    Dictionary<Type, List<Type>> HandlerIndex() {
        var index = new Dictionary<Type, List<Type>>();
        var all = Definition.HandlerBases.SelectMany(HandlersOf).Concat(Definition.Handlers).Distinct();
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
        var hooks = Definition.HookBases.SelectMany(Found).ToList();
        return new MessageRoute(message, handlers.TryGetValue(message, out var found) ? found : [], many) {
            Validators = ByName(Found(typeof(Validator<>)), message),
            BeforeSteps = Steps(Definition.BeforeSteps, typeof(BeforeStep<>), message),
            BeforeHooks = ByName(hooks.Where(h => typeof(IBeforeCall).IsAssignableFrom(h.Implementation)), message),
            AfterHooks = ByName(hooks.Where(h => typeof(IAfterCall).IsAssignableFrom(h.Implementation)), message),
            AfterSteps = Steps(Definition.AfterSteps, typeof(AfterStep<>), message),
        };
    }
}
