namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public async Task<int> Run(AppHost host) {
        var services = new ServiceCollection();
        Compose(services);
        await using var provider = services.BuildServiceProvider();
        var log = provider.GetRequiredService<IAppLog>();
        var converters = provider.GetServices<IOperationConverter>().ToList();
        if (converters.Count > 1) {
            log.Error($"{GetType().Name} has {converters.Count} operation converters: "
                + string.Join(", ", converters.Select(c => c.GetType().Name)) + ". Register one.");
            return 1;
        }
        if (converters.Count == 0) {
            log.Warning($"{GetType().Name} has no operation converter, so no host can reach its messages.");
        }
        var operations = new Operations(converters.SingleOrDefault(), provider.GetRequiredService<IServiceScopeFactory>());
        return await host.Run(operations).ConfigureAwait(false);
    }

    void Compose(IServiceCollection services) {
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
