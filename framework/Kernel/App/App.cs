namespace Umpire.Kernels;

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Markers;

[RequireDerived]
public abstract class App : IApp {
    const int NoThread = -1;

    static readonly SynchronizationContext threadPoolWhenNoAppThread = new();

    readonly object gate = new();
    AppHost? host;
    IServiceProvider? services;
    ILogger? log;
    AppState state;
    int generation;
    bool starting;
    int startThread = NoThread;
    int stopThread = NoThread;
    int runThread = NoThread;
    SynchronizationContext? appThread;
    bool stopRequested;
    int exitCode;
    Exception? stopError;
    ManualResetEventSlim stopRequest = new();
    ManualResetEventSlim stopped = new(initialState: true);

    public abstract Pack Pack { get; }

    public AppState State {
        get {
            lock (gate) {
                return state;
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

    public void Start(string[] args) => Start(args, ownedByRun: false);

    public int Run(string[] args) {
        Start(args, ownedByRun: true);
        ManualResetEventSlim request;
        lock (gate) {
            request = stopRequest;
        }
        request.Wait();
        return StopNow();
    }

    public void Stop() {
        var thread = Environment.CurrentManagedThreadId;
        int from;
        bool inline;
        bool deferred;
        ManualResetEventSlim done;
        lock (gate) {
            if ((state == AppState.Stopped && !starting) || stopThread == thread) {
                return;
            }
            from = generation;
            done = stopped;
            inline = !starting && state == AppState.Running && OnAppThread(thread);
            deferred = (starting && startThread == thread) || HostWork.IsCurrent;
        }
        if (inline) {
            StopNow();
            return;
        }
        OnRequest(from, 0);
        if (deferred) {
            return;
        }
        done.Wait();
        Exception? error;
        lock (gate) {
            error = from == generation ? stopError : null;
        }
        if (error is not null) {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    void Start(string[] args, bool ownedByRun) {
        var (current, done) = Begin(ownedByRun);
        var started = Pack.CreateHost();
        try {
            Launch(started, args, current);
        } catch {
            try {
                started.Dispose();
            } finally {
                EndFailedStart(done);
            }
            throw;
        }
        bool requested;
        lock (gate) {
            starting = false;
            startThread = NoThread;
            requested = stopRequested;
        }
        if (requested) {
            Dispatch(current);
        }
    }

    (int Generation, ManualResetEventSlim Stopped) Begin(bool ownedByRun) {
        lock (gate) {
            if (state != AppState.Stopped || starting) {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Call Stop() before Start or Run.");
            }
            starting = true;
            startThread = Environment.CurrentManagedThreadId;
            runThread = ownedByRun ? startThread : NoThread;
            appThread = ownedByRun ? null : SynchronizationContext.Current;
            stopRequested = false;
            exitCode = 0;
            stopError = null;
            stopRequest = new ManualResetEventSlim();
            stopped = new ManualResetEventSlim();
            return (++generation, stopped);
        }
    }

    void Launch(AppHost started, string[] args, int current) {
        var provider = started.Build(args, this);
        var stop = provider.GetRequiredService<AppStop>();
        lock (gate) {
            host = started;
            services = provider;
            log = provider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        }
        stop.Listen(code => OnRequest(current, code));
        started.Start(provider, stop);
        lock (gate) {
            state = AppState.Running;
        }
        OnStart();
    }

    void EndFailedStart(ManualResetEventSlim done) {
        lock (gate) {
            host = null;
            services = null;
            state = AppState.Stopped;
            starting = false;
            startThread = NoThread;
            runThread = NoThread;
        }
        done.Set();
    }

    void OnRequest(int from, int code) {
        lock (gate) {
            if (from != generation || stopRequested || (state != AppState.Running && !starting)) {
                return;
            }
            stopRequested = true;
            exitCode = code;
            if (starting) {
                return;
            }
        }
        Dispatch(from);
    }

    void Dispatch(int from) {
        ManualResetEventSlim request;
        SynchronizationContext? context;
        bool ownedByRun;
        lock (gate) {
            if (from != generation) {
                return;
            }
            request = stopRequest;
            context = appThread;
            ownedByRun = runThread != NoThread;
        }
        if (ownedByRun) {
            request.Set();
            return;
        }
        var target = context ?? threadPoolWhenNoAppThread;
        target.Post(_ => StopPosted(from, onThreadPool: context is null), null);
    }

    void StopPosted(int from, bool onThreadPool) {
        lock (gate) {
            if (from != generation) {
                return;
            }
        }
        try {
            StopNow(logErrors: onThreadPool);
        } catch when (onThreadPool) {
        }
    }

    int StopNow(bool logErrors = false) {
        AppHost? stopping;
        ManualResetEventSlim done;
        lock (gate) {
            if (state != AppState.Running || starting) {
                return exitCode;
            }
            state = AppState.Stopping;
            stopRequested = true;
            stopThread = Environment.CurrentManagedThreadId;
            stopping = host;
            done = stopped;
        }
        var error = RunStopSteps(stopping, logErrors);
        int code;
        lock (gate) {
            host = null;
            services = null;
            state = AppState.Stopped;
            stopThread = NoThread;
            runThread = NoThread;
            stopError = error;
            code = exitCode;
            log = null;
        }
        done.Set();
        if (error is not null) {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
        return code;
    }

    Exception? RunStopSteps(AppHost? stopping, bool logErrors) {
        Exception? error = null;
        try {
            OnStop();
        } catch (Exception e) {
            error = e;
            LogStopError(e, logErrors);
        }
        try {
            stopping?.Dispose();
        } catch (Exception e) {
            error ??= e;
            LogStopError(e, logErrors);
        }
        return error;
    }

    void LogStopError(Exception error, bool logErrors) {
        ILogger? logger;
        lock (gate) {
            logger = logErrors ? log : null;
        }
        try {
            logger?.LogError(error, "{App} failed to stop.", GetType().Name);
        } catch (ObjectDisposedException) {
        }
    }

    bool OnAppThread(int thread) =>
        runThread != NoThread
            ? thread == runThread
            : appThread is not null && SynchronizationContext.Current == appThread;
}
