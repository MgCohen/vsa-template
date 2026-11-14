using VSA.Kernel;

namespace VSA.Handlers;

sealed class PipelineCatalog {
    internal PipelineCatalog(AnchorTypes? types) {
        Types = types;
        Validators = Scan(typeof(Validator<>));
        Befores = Scan(typeof(Before<>));
        Afters = Scan(typeof(After<>));
    }

    internal static PipelineCatalog Empty { get; } = new(null);
    internal AnchorTypes? Types { get; }
    internal IReadOnlyList<(Type Message, Type Implementation)> Validators { get; }
    internal IReadOnlyList<(Type Message, Type Implementation)> Befores { get; }
    internal IReadOnlyList<(Type Message, Type Implementation)> Afters { get; }

    internal IEnumerable<Type> HandlersOf(Type handlerBase) =>
        Types is null
            ? throw new InvalidOperationException(
                $"Handlers<{handlerBase.Name}>() needs the module types, and these services have none. "
                + "Build the pipeline from the services of a VSA app, or list the handlers with Handlers(typeof(...)).")
            : Types.Implementations(handlerBase).Select(h => h.Implementation);

    IReadOnlyList<(Type Message, Type Implementation)> Scan(Type genericBase) =>
        Types is null
            ? []
            : [.. Types.Implementations(genericBase)
                .Select(h => (Message: h.Service.GetGenericArguments()[0], h.Implementation))];
}
