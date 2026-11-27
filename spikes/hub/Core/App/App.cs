namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public AppInstance Run(params AppHost[] hosts) {
        var services = new ServiceCollection();
        Compose(services);
        var provider = services.BuildServiceProvider();
        var log = provider.GetRequiredService<IAppLog>();
        var converters = provider.GetServices<IOperationConverter>().ToList();
        if (converters.Count > 1) {
            provider.Dispose();
            throw new InvalidOperationException($"{GetType().Name} has {converters.Count} operation converters: "
                + string.Join(", ", converters.Select(c => c.GetType().Name)) + ". Register one.");
        }
        if (converters.Count == 0) {
            log.Warning($"{GetType().Name} has no operation converter, so no host can reach its messages.");
        }
        var instance = new AppInstance(provider, new Operations(converters.SingleOrDefault(), provider.GetRequiredService<IServiceScopeFactory>()), log);
        instance.Open(hosts);
        return instance;
    }

    void Compose(IServiceCollection services) {
        services.AddSingleton<AppStop>();
        services.AddSingleton<MessageHub>();
        services.AddScoped<IHub>(scope => new ScopedHub(scope.GetRequiredService<MessageHub>(), scope));
        services.TryAddSingleton<IAppLog, ConsoleAppLog>();
        new TKernel().Install(services, GetType().Assembly);
        foreach (var module in GetType().Assembly.GetTypes()
            .Where(t => typeof(Module).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (Module)Activator.CreateInstance(t)!)) {
            module.Install(services);
        }
    }
}
