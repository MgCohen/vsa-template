namespace Spike.Core;

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

public abstract class App<TKernel> where TKernel : Kernel, new() {
    public AppInstance Run(params AppHost[] hosts) {
        var services = new ServiceCollection();
        services.AddSingleton<Hub>();
        services.AddScoped<IHub>(scope => new ScopedHub(scope.GetRequiredService<Hub>(), scope));
        new TKernel().Install(services, GetType().Assembly);
        foreach (var module in GetType().Assembly.GetTypes().Where(t => typeof(Module).IsAssignableFrom(t) && !t.IsAbstract)) {
            ((Module)Activator.CreateInstance(module)!).Install(services);
        }
        var instance = new AppInstance(services.BuildServiceProvider());
        foreach (var host in hosts) {
            instance.Open(host);
        }
        return instance;
    }
}

public sealed class AppInstance : IAsyncDisposable {
    readonly ServiceProvider provider;
    readonly IOperationConverter? converter;
    readonly List<AppHost> hosts = [];
    readonly TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal AppInstance(ServiceProvider provider) {
        this.provider = provider;
        converter = provider.GetService<IOperationConverter>();
        if (converter is null) {
            Console.Error.WriteLine("warning: the app has no operation converter, so no host can reach its messages.");
        }
    }

    public Task<int> Completion => completion.Task;

    public TaskAwaiter<int> GetAwaiter() => Completion.GetAwaiter();

    public async Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        if (converter is null) {
            return MessageResult.Fail($"No operation converter, so '{operation.Name}' has no message.");
        }
        var converted = converter.Convert(operation);
        return converted.Value is { } message ? await Send(message, ct).ConfigureAwait(false) : converted;
    }

    public async Task<MessageResult> Send(object message, CancellationToken ct = default) {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IHub>().Send(message, ct).ConfigureAwait(false);
    }

    public void Stop(int exitCode = 0) {
        if (completion.Task.IsCompleted) {
            return;
        }
        foreach (var host in Enumerable.Reverse(hosts)) {
            host.Close();
        }
        provider.Dispose();
        completion.TrySetResult(exitCode);
    }

    public ValueTask DisposeAsync() {
        Stop();
        return default;
    }

    internal void Open(AppHost host) {
        hosts.Add(host);
        host.Open(this);
    }
}

public abstract class AppHost {
    public abstract void Open(AppInstance app);

    public virtual void Close() {
    }
}

public abstract class Kernel {
    public void Install(IServiceCollection services, Assembly app) => Configure(services, app);

    protected abstract void Configure(IServiceCollection services, Assembly app);
}

public abstract class Module {
    public void Install(IServiceCollection services) => Configure(services);

    protected virtual void Configure(IServiceCollection services) {
    }
}
