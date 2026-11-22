namespace Umpire.Messages.Handlers;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Architecture;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

public sealed class Pipeline {
    readonly IServiceProvider services;
    readonly IRunner runner;
    readonly AppTypes? types;
    readonly bool many;
    readonly Dictionary<Type, List<Type>> handlers;
    readonly List<Type> openHandlers;
    readonly ConcurrentDictionary<Type, IReadOnlyList<RouteHandler>> matches = new();
    readonly ConcurrentDictionary<Type, MessageRoute> routes = new();

    internal Pipeline(PipelineDefinition definition, IServiceProvider services) {
        Definition = definition;
        this.services = services;
        types = services.GetService<AppTypes>();
        many = typeof(IBroadcastMessage).IsAssignableFrom(definition.Message);
        handlers = HandlerIndex();
        openHandlers = OpenHandlers();
        runner = CreateRunner(definition, services);
    }

    internal PipelineDefinition Definition { get; }

    public async Task<Result<TResult>> SendAsync<TResult>(object message, CancellationToken ct = default) =>
        EnvelopeResults.Of<TResult>(await RunAsync(message, services, ct));

    internal bool Runs(Type message) => many || HandlersFor(message).Count > 0;

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
        [.. MessageTypes.NearestPerDefinition(
                MessageTypes.NearestFirst(
                    found.Where(t => MessageTypes.Applies(MessageOf(t), message)), MessageOf),
                t => t.Implementation,
                MessageOf)
            .Select(t => t.Implementation)
            .Distinct()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)];

    static Type MessageOf(ServiceImplementation found) => found.Service.GetGenericArguments()[0];

    static Type[] Steps(IReadOnlyList<Type> steps, Type typedStepBase, Type message) =>
        [.. steps.Where(step => MessageTypes.Applies(MessageTypes.OfStep(step, typedStepBase), message))];

    IReadOnlyList<ServiceImplementation> Found(Type genericBase) =>
        types?.Implementations(genericBase) ?? [];

    IEnumerable<Type> HandlersOf(Type handlerBase) =>
        types is null
            ? throw new InvalidOperationException(
                $"HandlersOf({handlerBase.Name}) needs the module types, and these services have none. "
                + "Build the pipeline from the services of an Umpire app, "
                + "or list the handlers with Handlers(typeof(...)).")
            : types.Implementations(handlerBase).Select(h => h.Implementation);

    IEnumerable<Type> OpenHandlersOf(Type handlerBase) =>
        types?.OpenImplementations(handlerBase).Select(h => h.Implementation) ?? [];

    Dictionary<Type, List<Type>> HandlerIndex() {
        var index = new Dictionary<Type, List<Type>>();
        var all = Definition.HandlerBases.SelectMany(HandlersOf)
            .Concat(Definition.Handlers.Where(h => !h.IsGenericTypeDefinition))
            .Distinct();
        foreach (var handler in all) {
            foreach (var message in MessageTypes.OfHandler(handler)) {
                Add(index, message, [handler]);
            }
        }
        return index;
    }

    List<Type> OpenHandlers() {
        List<Type> listed = [.. Definition.Handlers.Where(h => h.IsGenericTypeDefinition)];
        listed.ForEach(MessageTypes.CheckOpenHandler);
        return [.. Definition.HandlerBases.SelectMany(OpenHandlersOf).Concat(listed).Distinct()];
    }

    void Add(Dictionary<Type, List<Type>> index, Type message, IEnumerable<Type> added) {
        if (!index.TryGetValue(message, out var list)) {
            index[message] = list = [];
        }
        foreach (var handler in added.Where(h => !list.Contains(h))) {
            list.Add(handler);
            if (!many && list.Count > 1) {
                throw new InvalidOperationException(
                    $"{message.FullName} has two handlers: {list[0].FullName} and {list[1].FullName}. "
                    + "A command or a request has one handler.");
            }
        }
    }

    IReadOnlyList<RouteHandler> HandlersFor(Type message) => matches.GetOrAdd(message, Match);

    IReadOnlyList<RouteHandler> Match(Type message) {
        var found = new Dictionary<Type, List<Type>>();
        foreach (var entry in handlers.Where(h => MessageTypes.Applies(h.Key, message))) {
            Add(found, entry.Key, entry.Value);
        }
        foreach (var closed in ClosedFor(message)) {
            foreach (var handled in MessageTypes.OfHandler(closed).Where(h => MessageTypes.Applies(h, message))) {
                Add(found, handled, [closed]);
            }
        }
        var all = MessageTypes.NearestPerDefinition(
                MessageTypes.NearestFirst(
                    found.SelectMany(f => f.Value.Select(h => new RouteHandler(f.Key, h))), r => r.Message),
                r => r.Handler,
                r => r.Message)
            .ToList();
        if (many) {
            return all;
        }
        var nearest = MessageTypes.Narrowest(all, r => r.Message);
        if (nearest.Count > 1) {
            throw new InvalidOperationException(
                $"{message.FullName} has two nearest handlers: "
                + $"{nearest[0].Handler.FullName} for {nearest[0].Message.Name} "
                + $"and {nearest[1].Handler.FullName} for {nearest[1].Message.Name}. "
                + $"Add a handler for {message.Name}, or remove one of them.");
        }
        return nearest.Count == 0 ? [] : [nearest[0]];
    }

    IReadOnlyList<string> CloseFailuresFor(Type message) => [.. openHandlers
        .SelectMany(open => MessageTypes.OfHandler(open)
            .SelectMany(pattern => MessageTypes.CloseFailures(open, pattern, message)))
        .Distinct()];

    IEnumerable<Type> ClosedFor(Type message) =>
        openHandlers.SelectMany(open => MessageTypes.OfHandler(open)
                .SelectMany(pattern => MessageTypes.Closed(open, pattern, message)))
            .Distinct();

    IEnumerable<ServiceImplementation> FoundFor(Type genericBase, Type message) =>
        Found(genericBase).Concat((types?.OpenImplementations(genericBase) ?? [])
            .SelectMany(open => ClosedFor(open, genericBase, message)));

    static IEnumerable<ServiceImplementation> ClosedFor(ServiceImplementation open, Type genericBase, Type message) =>
        MessageTypes.Closed(open.Implementation, open.Service.GetGenericArguments()[0], message)
            .Select(closed => (ClosedBase.Of(closed, genericBase) ?? ClosedBase.InterfaceOf(closed, genericBase)) is { } service
                ? new ServiceImplementation(service, closed)
                : null)
            .OfType<ServiceImplementation>();

    MessageRoute Route(Type message) {
        var hooks = Definition.HookBases.SelectMany(b => FoundFor(b, message)).ToList();
        return new MessageRoute(message, HandlersFor(message), many) {
            Validators = ByName(FoundFor(typeof(Validator<>), message), message),
            BeforeSteps = Steps(Definition.BeforeSteps, typeof(BeforeStep<>), message),
            BeforeHooks = ByName(hooks.Where(h => typeof(IBeforeCall).IsAssignableFrom(h.Implementation)), message),
            AfterHooks = ByName(hooks.Where(h => typeof(IAfterCall).IsAssignableFrom(h.Implementation)), message),
            AfterSteps = Steps(Definition.AfterSteps, typeof(AfterStep<>), message),
            CloseFailures = CloseFailuresFor(message),
        };
    }
}
