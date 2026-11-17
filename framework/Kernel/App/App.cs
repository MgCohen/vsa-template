namespace Umpire.Kernel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Markers;

[RequireDerived]
public abstract class App : IApp {
    readonly object gate = new();
    AppHost? running;
    bool starting;

    public abstract Pack Pack { get; }

    public bool IsRunning {
        get {
            lock (gate) {
                return running is not null;
            }
        }
    }

    public IServiceProvider? Services {
        get {
            lock (gate) {
                return running?.Services;
            }
        }
    }

    public int Run(string[] args) {
        lock (gate) {
            if (running is not null || starting) {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Run.");
            }
            starting = true;
        }
        var started = Start(args);
        var lifetime = started.Services?.GetService<IHostApplicationLifetime>();
        lifetime?.ApplicationStopped.Register(() => Stopped(started));
        try {
            OnStart();
            return started.Run();
        } catch (OperationCanceledException) when (lifetime?.ApplicationStopping.IsCancellationRequested == true) {
            Stopped(started);
            return 0;
        } catch {
            Clear(started);
            throw;
        }
    }

    public void Stop() {
        AppHost? stopped;
        lock (gate) {
            stopped = running;
        }
        stopped?.Stop();
    }

    protected internal virtual void ConfigureServices(IServiceCollection services) {
    }

    protected internal virtual IReadOnlyList<Module>? Modules() => null;

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    AppHost Start(string[] args) {
        try {
            var host = Pack.CreateHost();
            host.Create(args, this);
            lock (gate) {
                running = host;
            }
            return host;
        } finally {
            lock (gate) {
                starting = false;
            }
        }
    }

    void Stopped(AppHost stopped) {
        try {
            OnStop();
        } finally {
            Clear(stopped);
        }
    }

    void Clear(AppHost stopped) {
        lock (gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }
}
