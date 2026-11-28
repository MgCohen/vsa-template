namespace Composition.Tests;

using Greet;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

[NonParallelizable]
public sealed class ConsoleStopTests {
    static readonly TimeSpan limit = TimeSpan.FromSeconds(30);

    [Test]
    public void The_end_of_the_operation_stops_a_started_console_app() {
        var app = new TestApp<TestConsoleHost>(new GreetModule());

        app.Start("--name", "Ann", "--code", "3");

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void Ctrl_C_stops_a_started_console_app_and_cancels_the_operation() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        app.Start("wait");

        AppSignals.InterruptAllListeners();

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void Ctrl_C_ends_the_run_of_a_console_app_with_exit_code_zero() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        var run = Task.Run(() => app.RunToEnd("wait"));
        var deadline = DateTime.UtcNow + limit;
        while (app.State != AppState.Running && !run.IsCompleted && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }

        AppSignals.InterruptAllListeners();

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.Zero);
    }

    [Test]
    public void A_host_that_cancels_on_a_signal_ends_the_run_with_the_exit_code_of_the_operation() {
        var app = new TestApp<SignalConsoleHost>(new WaitModule());
        var run = Task.Run(() => app.RunToEnd("child"));
        var deadline = DateTime.UtcNow + limit;
        while (!ChildHandler.Waiting && !run.IsCompleted && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }

        AppSignals.InterruptAllListeners();

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.EqualTo(ChildHandler.ExitCode));
    }

    [Test]
    public void A_host_that_cancels_on_a_signal_ends_with_exit_code_zero_if_the_operation_throws_the_cancel() {
        var app = new TestApp<SignalConsoleHost>(new WaitModule());
        var run = Task.Run(() => app.RunToEnd("wait"));
        var deadline = DateTime.UtcNow + limit;
        while (app.State != AppState.Running && !run.IsCompleted && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }

        AppSignals.InterruptAllListeners();

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.Zero);
    }

    [Test]
    public void Stop_of_a_started_console_app_cancels_the_operation() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        app.Start("wait");

        Assert.That(Task.Run(app.Stop).Wait(limit), Is.True);
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
    }

    [Test]
    public void Stop_from_the_operation_ends_the_run_with_no_deadlock() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        HaltHandler.App = app;

        var run = Task.Run(() => app.RunToEnd("halt"));

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.Zero);
        Assert.That(HaltHandler.StopReturned, Is.True);
    }

    [Test]
    public void Stop_from_the_operation_of_a_started_app_stops_it_with_no_deadlock() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        HaltHandler.App = app;

        app.Start("halt");

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void A_cancellation_of_the_operation_itself_ends_the_run_with_exit_code_one() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());

        var run = Task.Run(() => app.RunToEnd("abort"));

        Assert.That(run.Wait(limit), Is.True);
        Assert.That(run.Result, Is.EqualTo(1));
    }

    static bool StopsInTime(IApp app) {
        var deadline = DateTime.UtcNow + limit;
        while (app.State != AppState.Stopped && DateTime.UtcNow < deadline) {
            Thread.Sleep(5);
        }
        return app.State == AppState.Stopped;
    }

    public sealed class WaitModule : Module;

    public sealed record WaitRequest : IRequest<WaitResponse>;

    public sealed record WaitResponse : IResponse;

    public sealed class WaitHandler : RequestHandler<WaitRequest, WaitResponse> {
        public override async Task<Result<WaitResponse>> HandleAsync(IEnvelope<WaitRequest> envelope) {
            await Task.Delay(Timeout.Infinite, envelope.Cancellation);
            return new WaitResponse();
        }
    }

    public sealed record ChildRequest : IRequest<ChildResponse>;

    public sealed record ChildResponse(int ExitCode) : IResponse, IExitCode;

    public sealed class ChildHandler : RequestHandler<ChildRequest, ChildResponse> {
        public const int ExitCode = 7;

        public static bool Waiting { get; private set; }

        public override async Task<Result<ChildResponse>> HandleAsync(IEnvelope<ChildRequest> envelope) {
            Waiting = true;
            try {
                await Task.Delay(Timeout.Infinite, envelope.Cancellation);
            } catch (OperationCanceledException) {
            } finally {
                Waiting = false;
            }
            return new ChildResponse(ExitCode);
        }
    }

    public sealed record HaltRequest : IRequest<WaitResponse>;

    public sealed class HaltHandler : RequestHandler<HaltRequest, WaitResponse> {
        public static TestApp<TestConsoleHost>? App { get; set; }

        public static bool StopReturned { get; private set; }

        public override Task<Result<WaitResponse>> HandleAsync(IEnvelope<HaltRequest> envelope) {
            StopReturned = false;
            if (!SpinWait.SpinUntil(() => App!.Running is not null, TimeSpan.FromSeconds(10))) {
                throw new TimeoutException("The run did not return its instance.");
            }
            App!.Stop();
            StopReturned = true;
            return Task.FromResult<Result<WaitResponse>>(new WaitResponse());
        }
    }

    public sealed record AbortRequest : IRequest<WaitResponse>;

    public sealed class AbortHandler : RequestHandler<AbortRequest, WaitResponse> {
        public override Task<Result<WaitResponse>> HandleAsync(IEnvelope<AbortRequest> envelope) =>
            throw new OperationCanceledException("The operation gave up.");
    }
}
