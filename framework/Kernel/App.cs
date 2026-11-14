using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;

namespace VSA.Kernel;

[RequireDerived]
public abstract class App {
    readonly object gate = new();
    Running? running;

    public bool IsRunning {
        get {
            lock (gate) {
                return running is not null;
            }
        }
    }

    public int Run(string[] args) => Start(args);

    public void Stop() {
        Running? stopped;
        lock (gate) {
            stopped = running;
        }
        stopped?.Host.Stop(stopped.App);
    }

    protected virtual int Start(string[] args) {
        var builder = Builder(args);
        var host = builder.SelectedHost();
        var app = builder.Build();
        var started = new Running(host, app);
        lock (gate) {
            if (running is not null) {
                app.Dispose();
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Run.");
            }
            running = started;
        }
        app.Services.GetService<IHostApplicationLifetime>()?.ApplicationStopped.Register(() => Clear(started));
        try {
            return host.Run(app);
        } catch {
            Clear(started);
            throw;
        }
    }

    protected virtual IHost Build(string[] args) => Builder(args).Build();

    protected virtual IAppHost Host() => new GenericAppHost();

    protected virtual IKeySource Keys() => new ConstraintKeys();

    protected virtual void Services(IServiceCollection services) { }

    protected virtual AppBuilder Configure(AppBuilder builder) => builder;

    AppBuilder Builder(string[] args) => Configure(AppBuilder.Create(args).UseHost(Host()).Keys(Keys()).Services(Services));

    void Clear(Running stopped) {
        lock (gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }

    sealed record Running(IAppHost Host, IHost App);
}
