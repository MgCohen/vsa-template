namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public abstract class App {
    readonly object gate = new();
    AppHost? host;
    ManualResetEventSlim stopRequest = new();
    bool ownedByRun;
    int exitCode;

    public abstract Kernel Kernel { get; }

    public AppState State { get; private set; }

    public THost Start<THost>(THost host, params string[] args) where THost : AppHost => Start(host, args, run: false);

    public int Run(AppHost host, params string[] args) {
        Start(host, args, run: true);
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
        try {
            OnStop();
            stopping?.Dispose();
        } finally {
            lock (gate) {
                host = null;
                State = AppState.Stopped;
            }
        }
        return exitCode;
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    THost Start<THost>(THost host, string[] args, bool run) where THost : AppHost {
        lock (gate) {
            if (State != AppState.Stopped) {
                throw new InvalidOperationException($"{GetType().Name} is already running. Call Stop() before Start.");
            }
            State = AppState.Starting;
            stopRequest = new ManualResetEventSlim();
            ownedByRun = run;
            exitCode = 0;
        }
        try {
            var services = host.Build(args, new AppParts(Kernel, GetType().Assembly));
            var stop = services.GetRequiredService<AppStop>();
            stop.Listen(OnStopRequest);
            host.Start(services, stop);
        } catch {
            host.Dispose();
            lock (gate) {
                State = AppState.Stopped;
            }
            throw;
        }
        lock (gate) {
            this.host = host;
            State = AppState.Running;
        }
        OnStart();
        return host;
    }

    void OnStopRequest(int code) {
        bool run;
        lock (gate) {
            exitCode = code;
            run = ownedByRun;
        }
        if (run) {
            stopRequest.Set();
        } else {
            ThreadPool.QueueUserWorkItem(_ => Stop());
        }
    }
}
