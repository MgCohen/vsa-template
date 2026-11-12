using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace VSA.Composition;

public abstract class VsaApp<TApp> : IArchitectureProvider where TApp : VsaApp<TApp>, new() {
    static readonly object Gate = new();
    static Running? running;

    public virtual ArchitectureDefinition Architecture => new VsaDefinition(Rules);

    public static bool IsRunning {
        get {
            lock (Gate) {
                return running is not null;
            }
        }
    }

    public static int Run(string[] args) => new TApp().Start(args);

    public static void Stop() {
        Running? stopped;
        lock (Gate) {
            stopped = running;
        }
        stopped?.Host.Stop(stopped.App);
    }

    protected virtual int Start(string[] args) {
        var builder = Builder(args).Services(Services);
        var host = builder.SelectedHost();
        var app = builder.Build();
        var started = new Running(host, app);
        lock (Gate) {
            if (running is not null) {
                app.Dispose();
                throw new InvalidOperationException(
                    $"{typeof(TApp).Name} is already running. Call {typeof(TApp).Name}.Stop() before Run.");
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

    protected virtual void Rules(RuleBook rules) { }

    static void Clear(Running stopped) {
        lock (Gate) {
            if (running == stopped) {
                running = null;
            }
        }
    }

    sealed record Running(IVsaHost Host, IHost App);
}
