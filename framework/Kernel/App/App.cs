namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Markers;

[RequireDerived]
public abstract class App : IApp {
    readonly object gate = new();
    AppHost? host;
    IServiceProvider? services;
    AppStop? waiting;
    bool starting;
    bool runs;
    bool stopAsked;

    public abstract Pack Pack { get; }

    public bool IsRunning {
        get {
            lock (gate) {
                return host is not null;
            }
        }
    }

    public IServiceProvider? Services {
        get {
            lock (gate) {
                return services;
            }
        }
    }

    public void Start(string[] args) {
        lock (gate) {
            if (host is not null || starting) {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Start or Run.");
            }
            starting = true;
        }
        var started = Pack.CreateHost();
        try {
            var provider = started.Build(args, this);
            lock (gate) {
                host = started;
                services = provider;
            }
            OnStart();
        } catch {
            Clear(started);
            started.Dispose();
            throw;
        } finally {
            lock (gate) {
                starting = false;
            }
        }
    }

    public int Run(string[] args) {
        lock (gate) {
            if (runs) {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Start or Run.");
            }
            runs = true;
            stopAsked = false;
        }
        try {
            Start(args);
        } catch {
            EndRun();
            throw;
        }
        try {
            var stop = Services!.GetRequiredService<AppStop>();
            lock (gate) {
                waiting = stop;
                if (stopAsked) {
                    stop.Request();
                }
            }
            using var signals = AppSignals.Listen(stop);
            return stop.Requested.GetAwaiter().GetResult();
        } finally {
            EndRun();
            StopHost();
        }
    }

    public void Stop() {
        lock (gate) {
            if (runs) {
                stopAsked = true;
                waiting?.Request();
                return;
            }
        }
        StopHost();
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    void EndRun() {
        lock (gate) {
            runs = false;
            waiting = null;
        }
    }

    void StopHost() {
        AppHost? stopped;
        lock (gate) {
            stopped = host;
        }
        if (stopped is null) {
            return;
        }
        try {
            OnStop();
        } finally {
            Clear(stopped);
            stopped.Dispose();
        }
    }

    void Clear(AppHost stopped) {
        lock (gate) {
            if (host == stopped) {
                host = null;
                services = null;
            }
        }
    }
}
