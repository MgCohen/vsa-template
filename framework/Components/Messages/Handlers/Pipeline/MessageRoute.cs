using System.Reflection;
using VSA.Analyzers.Markers;
using VSA.Contracts;

namespace VSA.Handlers;

sealed class MessageRoute {
    internal MessageRoute(Type message, IReadOnlyList<Type> handlers, bool many) {
        Message = message;
        Handlers = handlers;
        Many = many;
        Create = Creator(message);
    }

    internal Type Message { get; }
    internal IReadOnlyList<Type> Handlers { get; }
    internal bool Many { get; }
    internal Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope> Create { get; }
    internal IReadOnlyList<Type> Validators { get; init; } = [];
    internal IReadOnlyList<Type> BeforeSteps { get; init; } = [];
    internal IReadOnlyList<Type> BeforeHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterSteps { get; init; } = [];

    static Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope> Creator(Type message) =>
        (Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope>)typeof(MessageRoute)
            .GetMethod(nameof(CreateEnvelope), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(message)
            .CreateDelegate(typeof(Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope>));

    [Preserve]
    static Envelope CreateEnvelope<TMessage>(
        object message, Envelope? parent, IServiceProvider services, CancellationToken ct) where TMessage : class =>
        new Envelope<TMessage>((TMessage)message, parent, services, ct);
}
