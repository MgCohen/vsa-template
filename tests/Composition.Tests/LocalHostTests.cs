namespace Composition.Tests;

using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

[NonParallelizable]
public sealed class LocalHostTests {
    LocalApp<TestLocalHost> app = null!;

    [SetUp]
    public void StartApp() {
        app = new LocalApp<TestLocalHost>(new PingModule());
        app.Start([]);
    }

    [TearDown]
    public void StopApp() => app.Stop();

    [Test]
    public void Send_gives_the_message_result_with_the_response() {
        var result = app.Send((object)new PingRequest("Ann"));

        Assert.That(result.Value, Is.EqualTo(new PingResponse("Hello Ann")));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void Send_of_a_request_gives_the_typed_result() {
        Result<PingResponse> result = app.Send(new PingRequest("Ann"));

        Assert.That(result.Value.Text, Is.EqualTo("Hello Ann"));
    }

    [Test]
    public async Task SendAsync_of_a_request_gives_the_typed_result() {
        var result = await app.SendAsync(new PingRequest("Bo"));

        Assert.That(result.Value.Text, Is.EqualTo("Hello Bo"));
    }

    [Test]
    public void An_error_of_the_handler_is_the_error_of_the_result() {
        var typed = app.Send(new PingRequest(""));
        var untyped = app.Send((object)new PingRequest(""));

        Assert.That(typed.IsSuccess, Is.False);
        Assert.That(untyped.Error, Is.Not.Null);
    }

    [Test]
    public void A_message_with_no_operation_gives_a_clear_error() {
        var error = Assert.Throws<InvalidOperationException>(() => app.Send(new NotARequest()));

        Assert.That(error!.Message, Does.StartWith(
            $"TestLocalHost does not expose {typeof(NotARequest).FullName}. A host exposes only requests"));
    }

    [Test]
    public void Send_before_Start_gives_a_clear_error() {
        var stopped = new LocalApp<TestLocalHost>(new PingModule());

        var error = Assert.Throws<InvalidOperationException>(() => stopped.Send(new PingRequest("Ann")));

        Assert.That(error!.Message, Is.EqualTo("LocalApp does not run. Call Start before Send."));
    }

    [Test]
    public void The_app_is_ready_to_send_when_Start_returns() =>
        Assert.That(app.State, Is.EqualTo(AppState.Running));

    [Test]
    public void The_app_stops_and_starts_again() {
        app.Stop();

        Assert.That(app.State, Is.EqualTo(AppState.Stopped));
        Assert.Throws<InvalidOperationException>(() => app.Send(new PingRequest("Ann")));

        app.Start([]);

        Assert.That(app.Send(new PingRequest("Cy")).Value.Text, Is.EqualTo("Hello Cy"));
    }

    [Test]
    public void A_request_that_derives_from_a_handled_request_goes_to_that_handler() =>
        Assert.That(app.Send((object)new LoudPingRequest("Di")).Value, Is.EqualTo(new PingResponse("Hello Di")));

    [Test]
    public async Task Stop_waits_for_a_send_in_progress() {
        SlowHandler.Entered.Reset();
        SlowHandler.Release.Reset();
        var send = Task.Run(() => app.SendAsync(new SlowRequest()));
        Assert.That(SlowHandler.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);

        var stop = Task.Run(app.Stop);

        Assert.That(stop.Wait(TimeSpan.FromMilliseconds(200)), Is.False);
        SlowHandler.Release.Set();
        Assert.That(stop.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That((await send).Value, Is.EqualTo(new SlowResponse()));
    }

    [Test]
    public void Send_exists_only_for_an_app_with_a_LocalHost() {
        var hosts = new[] { typeof(LocalHostApps), typeof(LocalRequests) }
            .SelectMany(type => type.GetMethods())
            .Where(method => method.Name is "Send" or "SendAsync")
            .Select(method => method.GetGenericArguments()[1].GetGenericParameterConstraints().Single())
            .ToList();

        Assert.That(hosts, Has.Count.EqualTo(5).And.All.EqualTo(typeof(LocalHost)));
    }

    public sealed class PingModule : Module;

    public sealed record PingResponse(string Text) : IResponse;

    public record PingRequest(string Name) : IRequest<PingResponse>;

    public sealed record LoudPingRequest(string Name) : PingRequest(Name);

    public sealed record NotARequest;

    public sealed record SlowRequest : IRequest<SlowResponse>;

    public sealed record SlowResponse : IResponse;

    public sealed class SlowHandler : RequestHandler<SlowRequest, SlowResponse> {
        public static readonly ManualResetEventSlim Entered = new();
        public static readonly ManualResetEventSlim Release = new();

        public override Task<Result<SlowResponse>> HandleAsync(IEnvelope<SlowRequest> envelope) {
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(10));
            return Task.FromResult<Result<SlowResponse>>(new SlowResponse());
        }
    }

    public sealed class PingHandler : RequestHandler<PingRequest, PingResponse> {
        public override Task<Result<PingResponse>> HandleAsync(IEnvelope<PingRequest> envelope) =>
            Task.FromResult<Result<PingResponse>>(envelope.Message.Name.Length == 0
                ? new Error("ping.name", "Give a name.")
                : new PingResponse($"Hello {envelope.Message.Name}"));
    }
}
