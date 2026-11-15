namespace VSA.Kernel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;

[RequireDerived]
public abstract class App : IApp {
    readonly object gate = new();
    Running? running;

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
        var host = Pack.CreateHost();
        var app = host.Create(args, this);
        var started = new Running(host, app);
        lock (gate) {
            if (running is not null) {
                app.Dispose();
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Run.");
            }
            running = started;
        }
        app.Services.GetService<IHostApplicationLifetime>()?.ApplicationStopped.Register(() => Stopped(started));
        try {
            OnStart();
            return host.Run(app);
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
