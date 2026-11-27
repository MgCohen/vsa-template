namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public async Task<int> Run(AppHost host) {
        var services = new ServiceCollection();
        Compose(services);
        await using var provider = services.BuildServiceProvider();
        var converters = provider.GetServices<IOperationConverter>().ToList();
        if (converters.Count > 1) {
            await System.Console.Error.WriteLineAsync(
                $"error: {GetType().Name} has {converters.Count} operation converters: "
                + string.Join(", ", converters.Select(c => c.GetType().Name)) + ". Register one.").ConfigureAwait(false);
            return 1;
        }
        if (converters.Count == 0) {
            await System.Console.Error.WriteLineAsync(
                $"warning: {GetType().Name} has no operation converter, so no host can reach its messages.").ConfigureAwait(false);
        }
        var operations = new Operations(converters.SingleOrDefault(), provider.GetRequiredService<IHub>());
        return await host.Run(operations).ConfigureAwait(false);
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
