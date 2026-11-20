namespace Umpire.Messages.Handlers;

using System.Reflection;
using Umpire.Markers;
using Umpire.Messages.Contracts;

sealed class MessageRoute {
    internal MessageRoute(Type message, IReadOnlyList<Type> handlers, bool many) {
        Message = message;
        Handlers = handlers;
        Many = many;
        Create = Typed<Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope>>(
            nameof(CreateEnvelope), message);
        Call = Typed<Func<IHandler, Envelope, Task<IMessageResult>>>(nameof(CallHandler), message);
    }

    internal Type Message { get; }
    internal IReadOnlyList<Type> Handlers { get; }
    internal bool Many { get; }
    internal Func<object, Envelope?, IServiceProvider, CancellationToken, Envelope> Create { get; }
    internal Func<IHandler, Envelope, Task<IMessageResult>> Call { get; }
    internal IReadOnlyList<Type> Validators { get; init; } = [];
    internal IReadOnlyList<Type> BeforeSteps { get; init; } = [];
    internal IReadOnlyList<Type> BeforeHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterHooks { get; init; } = [];
    internal IReadOnlyList<Type> AfterSteps { get; init; } = [];

    static TDelegate Typed<TDelegate>(string method, Type message) where TDelegate : Delegate =>
        (TDelegate)typeof(MessageRoute)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(message)
            .CreateDelegate(typeof(TDelegate));

    [Preserve]
    static Envelope CreateEnvelope<TMessage>(
        object message, Envelope? parent, IServiceProvider services, CancellationToken ct) where TMessage : class =>
        new Envelope<TMessage>((TMessage)message, parent, services, ct);

    [Preserve]
    static Task<IMessageResult> CallHandler<TMessage>(IHandler handler, Envelope envelope) where TMessage : class =>
        ((IHandler<TMessage>)handler).RunAsync((Envelope<TMessage>)envelope);
}
