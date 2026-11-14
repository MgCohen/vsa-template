using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;
using VSA.Architecture;

namespace VSA.Composition;

[RequireDerived]
public abstract class VsaApp<TDefinition> : IArchitectureProvider where TDefinition : ArchitectureDefinition, new() {
    readonly object gate = new();
    Running? running;

    public virtual ArchitectureDefinition Architecture => new TDefinition();

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
        var builder = Builder(args).Services(Services);
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

    protected virtual IHost Build(string[] args) => Builder(args).Services(Services).Build();

    protected virtual VsaBuilder Builder(string[] args) => VsaBuilder.Create(args);

    protected virtual void Services(IServiceCollection services) { }

    void Clear(Running stopped) {
        lock (gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }

    sealed record Running(IVsaHost Host, IHost App);
}
