namespace Umpire.Kernels;

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    SynchronizationContext? appThread;
    bool stopRequested;
    int exitCode;
    Exception? stopError;
    ManualResetEventSlim stopped = new(initialState: true);
    TaskCompletionSource<int> completion = new();

    public abstract Kernel Kernel { get; }

    public AppState State {
        get {
            lock (gate) {
                return state;
            }
        }
    }

    protected internal IServiceProvider? Services {
        get {
            lock (gate) {
                return services;
            }
        }
    }

    public AppInstance Run(AppHost host) {
        if (host is null) {
            throw new ArgumentNullException(nameof(host));
        }
        var current = Begin();
        ManualResetEventSlim done;
        Task<int> completed;
        lock (gate) {
            done = stopped;
            completed = completion.Task;
        }
        try {
            Launch(host, current);
        } catch {
            try {
                host.Dispose();
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
        return new AppInstance(this, current, completed);
    }

    internal AppState StateOf(int run) {
        lock (gate) {
            return run == generation ? state : AppState.Stopped;
        }
    }

    internal void Stop(int run, int code) {
        var thread = Environment.CurrentManagedThreadId;
        bool inline;
        bool deferred;
        ManualResetEventSlim done;
        lock (gate) {
            if (run != generation || (state == AppState.Stopped && !starting) || stopThread == thread) {
                return;
            }
            done = stopped;
            inline = !starting && state == AppState.Running && OnAppThread();
            deferred = (starting && startThread == thread) || HostWork.IsCurrent;
            if (inline && !stopRequested) {
                exitCode = code;
            }
        }
        if (inline) {
            StopNow();
            return;
        }
        OnRequest(run, code);
        if (deferred) {
            return;
        }
        done.Wait();
        Exception? error;
        lock (gate) {
            error = run == generation ? stopError : null;
        }
        if (error is not null) {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    protected virtual void OnStart() {
    }

    protected virtual void OnStop() {
    }

    int Begin() {
        lock (gate) {
            if (state != AppState.Stopped || starting) {
                throw new InvalidOperationException(
                    $"{GetType().Name} is already running. Stop its AppInstance before the next Run.");
            }
            starting = true;
            startThread = Environment.CurrentManagedThreadId;
            appThread = SynchronizationContext.Current;
            stopRequested = false;
            exitCode = 0;
            stopError = null;
            stopped = new ManualResetEventSlim();
            completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            return ++generation;
        }
    }

    void Launch(AppHost started, int current) {
        var provider = started.Build(this);
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
        SynchronizationContext? context;
        lock (gate) {
            if (from != generation) {
                return;
            }
            context = appThread;
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
        TaskCompletionSource<int> completed;
        lock (gate) {
            if (state != AppState.Running || starting) {
                return exitCode;
            }
            completed = completion;
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
            stopError = error;
            code = exitCode;
            log = null;
        }
        done.Set();
        if (error is not null) {
            completed.TrySetException(error);
            ExceptionDispatchInfo.Capture(error).Throw();
        }
        completed.TrySetResult(code);
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

    bool OnAppThread() => appThread is not null && SynchronizationContext.Current == appThread;
}
