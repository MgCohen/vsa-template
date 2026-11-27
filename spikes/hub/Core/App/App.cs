namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public async Task<int> Run(AppHost host) {
        var services = new ServiceCollection();
        Compose(services);
        using var provider = services.BuildServiceProvider();
        CheckConverters(provider);
        return await host.Run(provider.GetRequiredService<IOperations>()).ConfigureAwait(false);
    }

    void Compose(IServiceCollection services) {
        services.AddSingleton<IHub, Hub>();
        services.AddSingleton<IOperations>(provider =>
            new Operations(provider.GetServices<IOperationConverter>().SingleOrDefault(), provider.GetRequiredService<IHub>()));
        new TKernel().Install(services, GetType().Assembly);
        foreach (var module in GetType().Assembly.GetTypes()
            .Where(t => typeof(Module).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (Module)Activator.CreateInstance(t)!)) {
            module.Install(services);
        }
    }

    void CheckConverters(IServiceProvider provider) {
        var converters = provider.GetServices<IOperationConverter>().ToList();
        if (converters.Count > 1) {
            throw new InvalidOperationException(
                $"{GetType().Name} has {converters.Count} operation converters: "
                + string.Join(", ", converters.Select(c => c.GetType().Name)) + ". Register one.");
        }
        if (converters.Count == 0) {
            System.Console.Error.WriteLine(
                $"warning: {GetType().Name} has no operation converter, so no host can reach its messages.");
        }
    }
}
