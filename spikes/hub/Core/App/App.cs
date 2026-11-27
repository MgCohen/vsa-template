namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public async Task<int> Run(AppHost host) {
        var services = new ServiceCollection();
        Compose(services);
        using var provider = services.BuildServiceProvider();
        return await host.Run(provider.GetRequiredService<IHub>()).ConfigureAwait(false);
    }

    void Compose(IServiceCollection services) {
        services.AddSingleton<IHub, Hub>();
        new TKernel().Install(services, GetType().Assembly);
        foreach (var module in GetType().Assembly.GetTypes()
            .Where(t => typeof(Module).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (Module)Activator.CreateInstance(t)!)) {
            module.Install(services);
        }
    }
}
