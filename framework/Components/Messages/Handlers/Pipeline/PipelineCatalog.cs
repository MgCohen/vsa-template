using VSA.Kernel;

namespace VSA.Handlers;

sealed class PipelineCatalog {
    internal PipelineCatalog(AppTypes? types) {
        Types = types;
        Validators = Scan(typeof(Validator<>));
    }

    internal static PipelineCatalog Empty { get; } = new(null);
    internal AppTypes? Types { get; }
    internal IReadOnlyList<StageType> Validators { get; }

    internal IEnumerable<Type> HandlersOf(Type handlerBase) =>
        Types is null
            ? throw new InvalidOperationException(
                $"Handlers<{handlerBase.Name}>() needs the module types, and these services have none. "
                + "Build the pipeline from the services of a VSA app, or list the handlers with Handlers(typeof(...)).")
            : Types.Implementations(handlerBase).Select(h => h.Implementation);

    internal IReadOnlyList<StageType> HooksOf(Type hookBase) => Scan(hookBase);

    IReadOnlyList<StageType> Scan(Type genericBase) =>
        Types is null
            ? []
            : [.. Types.Implementations(genericBase)
                .Select(h => new StageType(h.Service.GetGenericArguments()[0], h.Implementation))];
}
