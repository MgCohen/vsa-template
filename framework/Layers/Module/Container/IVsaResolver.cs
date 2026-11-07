namespace VSA.Module;

public interface IVsaResolver {
    object? Resolve(Type service);

    IEnumerable<object> ResolveAll(Type service);

    object Create(Type type);

    TService? Resolve<TService>() where TService : class => Resolve(typeof(TService)) as TService;

    TService Require<TService>() where TService : class =>
        Resolve<TService>() ?? throw new InvalidOperationException($"{typeof(TService).FullName} is not registered.");

    IEnumerable<TService> ResolveAll<TService>() => ResolveAll(typeof(TService)).Cast<TService>();
}
