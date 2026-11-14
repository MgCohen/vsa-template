using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Analyzers.Markers;
using VSA.Architecture;
using VSA.Module;

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

    protected virtual IVsaHost Host() => new GenericVsaHost();

    protected virtual IReadOnlyList<VSA.Module.Module>? Modules() => null;

    protected virtual IKeySource Keys() => new ConstraintKeys();

    protected virtual void Services(IServiceCollection services) { }

    VsaBuilder Builder(string[] args) {
        var builder = VsaBuilder.Create(args).UseHost(Host()).Keys(Keys()).Services(Services);
        return Modules() is { } modules ? builder.Modules([.. modules]) : builder;
    }

    void Clear(Running stopped) {
        lock (gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }

    sealed record Running(IVsaHost Host, IHost App);
}
