using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Contracts.Events;

namespace VSA.Handlers;

sealed class Pipeline<TMessage> : IPipeline<TMessage>, IPipelineRun where TMessage : class {
    readonly PipelineDefinition definition;
    readonly IServiceProvider services;
    readonly PipelineCatalog catalog;
    readonly IMessageIds ids;
    readonly bool many = typeof(IntegrationEvent).IsAssignableFrom(typeof(TMessage));
    readonly Dictionary<Type, List<Type>> handlers;
    readonly ConcurrentDictionary<Type, MessageRoute> routes = new();

    internal Pipeline(PipelineDefinition definition, IServiceProvider services) {
        this.definition = definition;
        this.services = services;
        catalog = services.GetService<PipelineCatalog>() ?? PipelineCatalog.Empty;
        ids = services.GetService<IMessageIds>() ?? new SequentialMessageIds();
        handlers = HandlerIndex();
        Runner = (IRunner)ActivatorUtilities.CreateInstance(services, definition.Runner);
    }

    public IRunner Runner { get; }
    public Type MessageBase => typeof(TMessage);

    public async Task<Result<TResult>> SendAsync<TResult>(TMessage message, CancellationToken ct = default) =>
        EnvelopeResults.Of<TResult>(await RunAsync(message, services, ct));

    public Task StartAsync() => Runner.StartAsync();

    public bool Runs(Type message) => many || handlers.ContainsKey(message);

    public async Task<Envelope> RunAsync(object message, IServiceProvider scope, CancellationToken ct) {
        if (message is not TMessage) {
            throw new ArgumentException(
                $"{message?.GetType().FullName ?? "null"} is not a {typeof(TMessage).Name}, "
                + "so this pipeline cannot run it.",
                nameof(message));
        }
        var route = routes.GetOrAdd(message.GetType(), Route);
        var envelope = route.Factory.Create(message, ids.Next(), CurrentEnvelope.Value, scope, ct);
        await Runner.RunAsync(envelope, e => PipelineRun.ExecuteAsync(route, e));
        return envelope;
    }

    static Type[] Matching(IReadOnlyList<(Type Message, Type Implementation)> types, Type message) =>
        [.. types.Where(t => t.Message.IsAssignableFrom(message)).Select(t => t.Implementation)];

    static Type[] Steps(IReadOnlyList<Type> steps, Type stepBase, Type message) =>
        [.. steps.Where(step => MessageTypes.OfStep(step, stepBase).IsAssignableFrom(message))];

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
        var factory = (IEnvelopeFactory)Activator.CreateInstance(typeof(EnvelopeFactory<>).MakeGenericType(message))!;
        return new MessageRoute(message, handlers.TryGetValue(message, out var found) ? found : [], many, factory) {
            Validators = Matching(catalog.Validators, message),
            BeforeSteps = Steps(definition.BeforeSteps, typeof(BeforeStep<>), message),
            BeforeEvents = Matching(catalog.Befores, message),
            AfterEvents = Matching(catalog.Afters, message),
            AfterSteps = Steps(definition.AfterSteps, typeof(AfterStep<>), message),
        };
    }

}
