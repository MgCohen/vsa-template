namespace Framework.Tests.Host;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umpire.Kernels;

[NonParallelizable]
public sealed class AppStopTests {
    static readonly TimeSpan limit = TimeSpan.FromSeconds(30);

    [Test]
    public void A_stop_request_stops_a_started_app() {
        var app = new StopApp();
        app.Start();

        app.Services!.GetRequiredService<AppStop>().Request(4);

        Assert.That(StopsInTime(app), Is.True);
        Assert.That(app.Stops, Is.EqualTo(1));
    }

    [Test]
    public void A_stop_request_in_OnStart_ends_Run_with_its_exit_code() {
        var app = new StopApp { RequestOnStart = 6 };

        Assert.That(app.RunToEnd(), Is.EqualTo(6));
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
    }

    [Test]
    public void Only_the_first_stop_request_gives_the_exit_code() {
        var app = new StopApp();
        var run = Task.Run(() => app.RunToEnd());
        WaitUntilRunning(app, run);
        var stop = app.Services!.GetRequiredService<AppStop>();

        stop.Request(3);
        stop.Request(8);

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.EqualTo(3));
    }

    [Test]
    public void A_stop_request_from_another_thread_runs_the_stop_on_the_app_thread() {
        var context = new QueueContext();
        using var appThread = context.Enter();
        var app = new StopApp();
        app.Start();

        Task.Run(() => app.Services!.GetRequiredService<AppStop>().Request()).Wait(limit);
        var beforeThePost = app.State;
        context.RunPending();

        Assert.That(beforeThePost, Is.EqualTo(AppState.Running));
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
        Assert.That(app.StopThread, Is.EqualTo(Environment.CurrentManagedThreadId));
    }

    [Test]
    public void Stop_on_the_app_thread_runs_the_stop_inline_with_no_post() {
        var context = new QueueContext();
        using var appThread = context.Enter();
        var app = new StopApp();
        app.Start();

        app.Stop();

        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
        Assert.That(context.Pending, Is.Zero);
    }

    [Test]
    public void Stop_on_another_thread_waits_until_the_app_thread_runs_the_stop() {
        var context = new QueueContext();
        using var appThread = context.Enter();
        var app = new StopApp();
        app.Start();

        var stop = Task.Run(app.Stop);
        var deadline = DateTime.UtcNow + limit;
        while (!stop.IsCompleted && DateTime.UtcNow < deadline) {
            context.RunPending();
            Thread.Sleep(5);
        }

        Assert.That(stop.IsCompletedSuccessfully, Is.True);
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
        Assert.That(app.StopThread, Is.EqualTo(Environment.CurrentManagedThreadId));
    }

    [Test]
    public void A_stop_request_in_OnStart_runs_after_Start_ends() {
        var context = new QueueContext();
        using var appThread = context.Enter();
        var app = new StopApp { RequestOnStart = 0 };

        app.Start();
        var afterStart = app.State;
        context.RunPending();

        Assert.That(afterStart, Is.EqualTo(AppState.Running));
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
    }

    [Test]
    public void A_stop_request_of_an_earlier_start_does_not_stop_the_app() {
        var app = new StopApp();
        app.Start();
        var earlier = app.Services!.GetRequiredService<AppStop>();
        app.Stop();
        app.Start();

        earlier.Request();

        Assert.That(app.State, Is.EqualTo(AppState.Running));
        app.Stop();
    }

    [Test]
    public void An_app_with_the_base_host_does_not_listen_for_Ctrl_C() {
        var app = new StopApp();
        app.Start();

        AppSignals.InterruptAllListeners();

        Assert.That(app.State, Is.EqualTo(AppState.Running));
        app.Stop();
    }

    [Test]
    public void Ctrl_C_stops_a_started_web_app() {
        var app = WebApps.OnTestServer();
        app.RunOnTestServer();

        AppSignals.InterruptAllListeners();

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void Ctrl_C_ends_the_run_of_a_web_app() {
        var app = WebApps.OnTestServer();
        var run = Task.Run(() => app.RunToEnd());
        WaitUntilRunning(app, run);

        AppSignals.InterruptAllListeners();

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.Zero);
    }

    [Test]
    public void StopApplication_stops_a_started_web_app() {
        var app = WebApps.OnTestServer();
        app.RunOnTestServer();

        app.Services!.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void StopApplication_ends_the_run_of_a_web_app() {
        var app = WebApps.OnTestServer();
        var run = Task.Run(() => app.RunToEnd());
        WaitUntilRunning(app, run);

        app.Services!.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.Zero);
    }

    [Test]
    public void A_stop_request_ends_the_run_of_a_web_app_with_its_exit_code() {
        var app = WebApps.OnTestServer();
        var run = Task.Run(() => app.RunToEnd());
        WaitUntilRunning(app, run);

        app.Services!.GetRequiredService<AppStop>().Request(9);

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.EqualTo(9));
    }

    [Test]
    public void An_OnStop_error_in_a_stop_on_the_thread_pool_goes_to_the_log() {
        using var logs = new CapturedLogs();
        var app = new StopApp(() => new LogHost(logs) { ModuleProvider = new FixedModules() }) { ThrowOnStop = true };
        Task.Run(() => app.Start()).Wait(limit);

        app.Services!.GetRequiredService<AppStop>().Request();

        Assert.That(StopsInTime(app), Is.True);
        Assert.That(logs.Errors, Has.One.EqualTo("StopApp failed to stop. OnStop failed."));
    }

    [Test]
    public void An_OnStop_error_goes_to_the_caller_of_Stop() {
        var app = new StopApp { ThrowOnStop = true };
        app.Start();

        var error = Assert.Throws<InvalidOperationException>(app.Stop);

        Assert.That(error!.Message, Is.EqualTo("OnStop failed."));
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
    }

    static void WaitUntilRunning(IApp app, Task run) {
        var deadline = DateTime.UtcNow + limit;
        while (app.State != AppState.Running && !run.IsCompleted && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }
        Assert.That(app.State, Is.EqualTo(AppState.Running));
    }

    static bool StopsInTime(IApp app) {
        var deadline = DateTime.UtcNow + limit;
        while (app.State != AppState.Stopped && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }
        return app.State == AppState.Stopped;
    }

    sealed class StopApp(Func<AppHost>? host = null)
        : VsaTestApp(host ?? (() => new AppHost { ModuleProvider = new FixedModules() })) {
        public int? RequestOnStart { get; init; }

        public bool ThrowOnStop { get; init; }

        public int Stops { get; private set; }

        public int StopThread { get; private set; }

        protected override void OnStart() {
            if (RequestOnStart is { } code) {
                Services!.GetRequiredService<AppStop>().Request(code);
            }
        }

        protected override void OnStop() {
            Stops++;
            StopThread = Environment.CurrentManagedThreadId;
            if (ThrowOnStop) {
                throw new InvalidOperationException("OnStop failed.");
            }
        }
    }

    sealed class LogHost(CapturedLogs logs) : AppHost {
        protected override void Configure(IServiceCollection services, AppTypes types) =>
            services.AddLogging(logging => logging.AddProvider(logs));
    }

    sealed class CapturedLogs : ILoggerProvider, ILogger {
        readonly List<string> errors = [];

        public IReadOnlyList<string> Errors {
            get {
                lock (errors) {
                    return [.. errors];
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel == LogLevel.Error) {
                lock (errors) {
                    errors.Add($"{formatter(state, exception)} {exception?.Message}");
                }
            }
        }

        public void Dispose() {
        }
    }
}
