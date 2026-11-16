namespace Umpire.Kernel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Markers;

[RequireDerived]
public abstract class App : IApp {
    readonly object gate = new();
    Running? running;
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
                return running?.App.Services;
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
        var lifetime = started.App.Services.GetService<IHostApplicationLifetime>();
        lifetime?.ApplicationStopped.Register(() => Stopped(started));
        try {
            OnStart();
            return started.Host.Run(started.App);
        } catch (OperationCanceledException) when (lifetime?.ApplicationStopping.IsCancellationRequested == true) {
            Stopped(started);
            return 0;
        } catch {
            Clear(started);
            throw;
        }
    }

    public void Stop() {
        Running? stopped;
        lock (gate) {
            stopped = running;
        }
        stopped?.Host.Stop(stopped.App);
    }

    protected internal virtual void ConfigureServices(IServiceCollection services) {
    }

    protected internal virtual IKeySource Keys() => new ConstraintKeys();

    protected internal virtual IReadOnlyList<Module>? Modules() => null;

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    Running Start(string[] args) {
        try {
            var host = Pack.CreateHost();
            var started = new Running(host, host.Create(args, this));
            lock (gate) {
                running = started;
            }
            return started;
        } finally {
            lock (gate) {
                starting = false;
            }
        }
    }

    void Stopped(Running stopped) {
        try {
            OnStop();
        } finally {
            Clear(stopped);
        }
    }

    void Clear(Running stopped) {
        lock (gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }

    sealed record Running(AppHost Host, IHost App);
}
