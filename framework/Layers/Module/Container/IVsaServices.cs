namespace VSA.Module;

public interface IVsaServices {
    void Add(Type service, Type implementation, VsaLifetime lifetime);

    void Add(Type service, Func<IVsaResolver, object> factory, VsaLifetime lifetime);

    void AddInstance(Type service, object instance);

    VsaLifetime? LifetimeOf(Type service);

    void RemoveAll(Type service);

    bool Contains(Type service) => LifetimeOf(service) is not null;

    void AddSingleton<TService>() where TService : class =>
        Add(typeof(TService), typeof(TService), VsaLifetime.Singleton);

    void AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService =>
        Add(typeof(TService), typeof(TImplementation), VsaLifetime.Singleton);

    void AddSingleton<TService>(TService instance) where TService : class =>
        AddInstance(typeof(TService), instance);

    void AddSingleton<TService>(Func<IVsaResolver, TService> factory) where TService : class =>
        Add(typeof(TService), resolver => factory(resolver), VsaLifetime.Singleton);

    void AddScoped<TService>() where TService : class =>
        Add(typeof(TService), typeof(TService), VsaLifetime.Scoped);

    void AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService =>
        Add(typeof(TService), typeof(TImplementation), VsaLifetime.Scoped);

    void AddScoped<TService>(Func<IVsaResolver, TService> factory) where TService : class =>
        Add(typeof(TService), resolver => factory(resolver), VsaLifetime.Scoped);

    void AddTransient<TService>() where TService : class =>
        Add(typeof(TService), typeof(TService), VsaLifetime.Transient);

    void AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService =>
        Add(typeof(TService), typeof(TImplementation), VsaLifetime.Transient);
}
