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

        app.Start(["--name", "Ann", "--code", "3"]);

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void Ctrl_C_stops_a_started_console_app_and_cancels_the_operation() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        app.Start(["wait"]);

        AppSignals.InterruptAllListeners();

        Assert.That(StopsInTime(app), Is.True);
    }

    [Test]
    public void Ctrl_C_ends_the_run_of_a_console_app_with_exit_code_zero() {
        var app = new TestApp<TestConsoleHost>(new WaitModule());
        var run = Task.Run(() => app.Run(["wait"]));
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
        app.Start(["wait"]);

        Assert.That(Task.Run(app.Stop).Wait(limit), Is.True);
        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
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
        public override async Task<Result<WaitResponse>> HandleAsync(Envelope<WaitRequest> envelope) {
            await Task.Delay(Timeout.Infinite, envelope.Cancellation);
            return new WaitResponse();
        }
    }
}
