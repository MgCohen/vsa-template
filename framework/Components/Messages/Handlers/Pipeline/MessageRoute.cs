namespace Umpire.Messages.Handlers;

using System.Reflection;
using Umpire.Messages.Contracts;

sealed class MessageRoute {
    readonly Dictionary<RouteHandler, IHandlerCall?> calls;
    readonly IReadOnlyList<AfterHandledCall> afterHandled;

    internal MessageRoute(
        Type message,
        IReadOnlyList<RouteHandler> handlers,
        bool many,
        IReadOnlyList<Type> callTypes,
        IReadOnlyList<AfterHandledCall> afterHandled) {
        Message = message;
        Handlers = handlers;
        Many = many;
        Create = Typed<Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope>>(
            nameof(CreateEnvelope), message);
        calls = handlers.Distinct().ToDictionary(handler => handler, handler => CallFor(handler, callTypes));
        this.afterHandled = afterHandled;
    }

    internal Type Message { get; }
    internal IReadOnlyList<RouteHandler> Handlers { get; }
    internal bool Many { get; }
    internal Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope> Create { get; }
    internal IReadOnlyList<Type> BeforeValidationSteps { get; init; } = [];
    internal IReadOnlyList<Type> Validators { get; init; } = [];
    internal IReadOnlyList<Type> BeforeSteps { get; init; } = [];
    internal IReadOnlyList<Type> BeforeHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterSteps { get; init; } = [];
    internal IReadOnlyList<string> CloseFailures { get; init; } = [];

    internal async Task<IMessageResult> CallAsync(RouteHandler handler, object instance, Envelope envelope) {
        var call = calls[handler] ?? throw NoCall(handler);
        var result = await call.CallAsync(instance, envelope);
        foreach (var after in afterHandled.Where(after => after.Handler.IsInstanceOfType(instance))) {
            after.Call(instance);
        }
        return result;
    }

    static IHandlerCall? CallFor(RouteHandler handler, IReadOnlyList<Type> callTypes) =>
        callTypes
            .SelectMany(open => HandlerCalls.Close(open, handler.Handler, handler.Message))
            .Select(closed => (IHandlerCall?)Activator.CreateInstance(closed))
            .FirstOrDefault();

    static InvalidOperationException NoCall(RouteHandler handler) =>
        new($"The pipeline has no call for {handler.Handler.FullName}, the handler of {handler.Message.Name}. "
            + "Add the call of its handler kind to the pipeline, for example CallsCommandHandlers(), "
            + "or Calls(typeof(...)) with a HandlerCall<THandler, TMessage> that fits the handler.");

    static TDelegate Typed<TDelegate>(string method, Type message) where TDelegate : Delegate =>
        (TDelegate)typeof(MessageRoute)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(message)
            .CreateDelegate(typeof(TDelegate));

    static Envelope CreateEnvelope<TMessage>(
        object message, Envelope? parent, IServiceProvider services, CancellationToken ct) where TMessage : class =>
        new Envelope<TMessage>((TMessage)message, parent, services, ct);
}
