namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public abstract class App {
    readonly object gate = new();
    ServiceProvider? services;
    IReadOnlyList<AppHost> hosts = [];
    TaskCompletionSource<int> stopped = new();

    public abstract Kernel Kernel { get; }

    public IModuleProvider? Modules { get; init; }

    public AppState State { get; private set; }

    public Task<int> Run(params AppHost[] hosts) {
        lock (gate) {
            if (State != AppState.Stopped) {
                throw new InvalidOperationException($"{GetType().Name} is already running. Call Stop() before Run.");
            }
            State = AppState.Starting;
            stopped = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        try {
            var collection = new ServiceCollection();
            new Composition(Kernel, Modules ?? new ScannedModules(GetType().Assembly)).Install(collection);
            services = collection.BuildServiceProvider();
            var operations = services.GetRequiredService<IOperations>();
            var stop = services.GetRequiredService<AppStop>();
            stop.Listen(code => ThreadPool.QueueUserWorkItem(_ => Stop(code)));
            this.hosts = hosts;
            foreach (var host in hosts) {
                host.Open(operations, stop);
            }
        } catch {
            Close();
            lock (gate) {
                State = AppState.Stopped;
            }
            throw;
        }
        lock (gate) {
            State = AppState.Running;
        }
        OnStart();
        return stopped.Task;
    }

    public void Stop(int exitCode = 0) {
        lock (gate) {
            if (State != AppState.Running) {
                return;
            }
            State = AppState.Stopping;
        }
        try {
            OnStop();
            Close();
        } finally {
            lock (gate) {
                State = AppState.Stopped;
            }
            stopped.TrySetResult(exitCode);
        }
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    void Close() {
        foreach (var host in hosts.Reverse()) {
            host.Dispose();
        }
        hosts = [];
        services?.Dispose();
        services = null;
    }
}
