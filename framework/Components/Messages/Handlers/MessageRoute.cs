namespace VSA.Handlers;

sealed class MessageRoute {
    internal MessageRoute(Type message, IReadOnlyList<Type> handlers, bool many, IEnvelopeFactory factory) {
        Message = message;
        Handlers = handlers;
        Many = many;
        Factory = factory;
    }

    internal Type Message { get; }
    internal IReadOnlyList<Type> Handlers { get; }
    internal bool Many { get; }
    internal IEnvelopeFactory Factory { get; }
    internal IReadOnlyList<Type> Validators { get; init; } = [];
    internal IReadOnlyList<Type> BeforeSteps { get; init; } = [];
    internal IReadOnlyList<Type> BeforeEvents { get; init; } = [];
    internal IReadOnlyList<Type> AfterEvents { get; init; } = [];
    internal IReadOnlyList<Type> AfterSteps { get; init; } = [];
}
