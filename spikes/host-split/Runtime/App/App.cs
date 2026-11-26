namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public abstract class App {
    readonly object gate = new();
    AppHost? host;
    ManualResetEventSlim stopRequest = new();
    int exitCode;

    public abstract Kernel Kernel { get; }

    public AppState State { get; private set; }

    public THost Start<THost>(THost host, params string[] args) where THost : AppHost {
        lock (gate) {
            if (State != AppState.Stopped) {
                throw new InvalidOperationException($"{GetType().Name} is already running. Call Stop() before Start.");
            }
            stopRequest = new ManualResetEventSlim();
            exitCode = 0;
        }
        var services = host.Build(args, new AppParts(Kernel, GetType().Assembly));
        var stop = services.GetRequiredService<AppStop>();
        stop.Listen(OnStopRequest);
        host.Start(services, stop);
        lock (gate) {
            this.host = host;
            State = AppState.Running;
        }
        OnStart();
        return host;
    }

    public int Run(AppHost host, params string[] args) {
        Start(host, args);
        stopRequest.Wait();
        return Stop();
    }

    public int Stop() {
        AppHost? stopping;
        lock (gate) {
            if (State != AppState.Running) {
                return exitCode;
            }
            State = AppState.Stopping;
            stopping = host;
        }
        OnStop();
        stopping?.Dispose();
        lock (gate) {
            host = null;
            State = AppState.Stopped;
            return exitCode;
        }
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    void OnStopRequest(int code) {
        exitCode = code;
        stopRequest.Set();
    }
}
