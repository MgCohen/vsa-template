namespace Composition.Tests.Hub;

using Composition.Tests.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Handlers.Requests;

public sealed class HubStepTests {
    [Test]
    public async Task A_hub_step_runs_before_and_after_a_command() {
        using var services = Services(added => added.AddHubStep<OuterStep>());

        var result = await Commands(services).SendAsync(new Hit(2), default);

        Assert.That(result.Value, Is.EqualTo(2));
        Assert.That(Log(services), Is.EqualTo(new[] { "outer before Hit", "hit 2", "outer after ok" }));
    }

    [Test]
    public async Task Hub_steps_run_in_registration_order_and_their_after_parts_in_reverse() {
        using var services = Services(added => added.AddHubStep<OuterStep>().AddHubStep<InnerStep>());

        await Commands(services).SendAsync(new Hit(1), default);

        Assert.That(Log(services), Is.EqualTo(new[] {
            "outer before Hit", "inner before Hit", "hit 1", "inner after ok", "outer after ok",
        }));
    }

    [Test]
    public async Task A_hub_step_that_refuses_stops_the_message_and_the_earlier_steps_end() {
        using var services = Services(added => added.AddHubStep<OuterStep>().AddHubStep<RefuseStep>().AddHubStep<InnerStep>());

        var result = await Commands(services).SendAsync(new Hit(3), default);

        Assert.That(result.Error?.Code, Is.EqualTo("hub.closed"));
        Assert.That(Log(services), Is.EqualTo(new[] { "outer before Hit", "refuse Hit", "outer after hub.closed" }));
    }

    [Test]
    public void The_after_part_runs_when_the_handler_throws() {
        using var services = Services(added => added.AddHubStep<OuterStep>());

        Assert.ThrowsAsync<InvalidOperationException>(() => Commands(services).SendAsync(new Hit(Hit.Throws), default));

        Assert.That(Log(services), Is.EqualTo(new[] { "outer before Hit", "hit -1", "outer after none" }));
    }

    [Test]
    public async Task A_hub_step_runs_for_a_request_and_for_its_inner_send() {
        using var services = Services(added => added.AddHubStep<OuterStep>());
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IRequestSender>().SendAsync(new Score(), default);

        Assert.That(Log(services), Is.EqualTo(new[] {
            "outer before Score", "score", "outer before Hit", "hit 5", "outer after ok", "outer after ok",
        }));
    }

    [Test]
    public async Task A_hub_step_runs_one_time_for_an_event_that_two_pipelines_run() {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules()));
        services.AddSingleton<HubLog>();
        services.AddEventHandlers();
        services.AddPipeline(new PipelineBuilder<Scored>().CallsTestHandlers().Handlers(typeof(ScoredHandler)));
        services.AddPipeline(new PipelineBuilder<IntegrationEvent>().CallsTestHandlers().Handlers(typeof(AuditHandler)));
        services.AddHubStep<OuterStep>();
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Scored(), default);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(Log(provider), Is.EqualTo(new[] { "outer before Scored", "scored", "audit", "outer after ok" }));
    }

    [Test]
    public async Task The_hub_sends_a_message_by_its_type() {
        using var services = Services(added => added.AddHubStep<OuterStep>());
        using var scope = services.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IMessageHub>();

        var command = await hub.SendAsync(new Hit(4));
        var request = await hub.SendAsync(new Score());

        Assert.That(command.Value, Is.EqualTo(4));
        Assert.That(request.Value, Is.EqualTo(new ScoreResponse(5)));
    }

    [Test]
    public async Task The_hub_gives_a_rejected_message_as_an_error() {
        using var services = Services(_ => { });
        using var scope = services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMessageHub>().SendAsync(new Hit(Hit.Rejected));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error?.Code, Is.EqualTo("hit.rejected"));
    }

    [Test]
    public void An_after_part_that_throws_does_not_stop_the_earlier_after_parts() {
        using var services = Services(added => added.AddHubStep<OuterStep>().AddHubStep<BrokenAfterStep>());

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Commands(services).SendAsync(new Hit(1), default));

        Assert.That(error!.Message, Is.EqualTo("broken after"));
        Assert.That(Log(services), Is.EqualTo(new[] { "outer before Hit", "hit 1", "broken after", "outer after ok" }));
    }

    [Test]
    public void A_before_part_that_throws_ends_the_earlier_steps_and_the_message_does_not_run() {
        using var services = Services(added => added.AddHubStep<OuterStep>().AddHubStep<BrokenBeforeStep>());

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Commands(services).SendAsync(new Hit(1), default));

        Assert.That(error!.Message, Is.EqualTo("broken before"));
        Assert.That(Log(services), Is.EqualTo(new[] { "outer before Hit", "broken before", "outer after none" }));
    }

    [Test]
    public async Task Each_hub_step_gets_the_token_of_the_send() {
        using var services = Services(added => added.AddHubStep<TokenStep>());
        using var source = new CancellationTokenSource();

        await Commands(services).SendAsync(new Hit(1), source.Token);

        Assert.That(services.GetRequiredService<HubLog>().Tokens, Is.EqualTo(new[] { source.Token, source.Token }));
    }

    [Test]
    public async Task A_lower_priority_runs_first() {
        using var services = Services(added => added.AddHubStep<OuterStep>().AddHubStep<InnerStep>(priority: -1));

        await Commands(services).SendAsync(new Hit(1), default);

        Assert.That(Log(services), Is.EqualTo(new[] {
            "inner before Hit", "outer before Hit", "hit 1", "outer after ok", "inner after ok",
        }));
    }

    [Test]
    public void A_hub_step_is_added_one_time() {
        var services = new ServiceCollection().AddHubStep<OuterStep>();

        Assert.That(() => services.AddHubStep<OuterStep>(),
            Throws.InvalidOperationException.With.Message.Contains("is already a hub step"));
    }

    [Test]
    public void The_hub_names_a_message_with_no_handler() {
        using var services = Services(_ => { });
        using var scope = services.CreateScope();

        Assert.That(() => scope.ServiceProvider.GetRequiredService<IMessageHub>().SendAsync(new Unknown()),
            Throws.InvalidOperationException.With.Message.Contains("No pipeline has a handler for Unknown"));
    }

    [Test]
    public async Task The_hub_publishes_an_event_that_it_gets_by_type() {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules()));
        services.AddSingleton<HubLog>();
        services.AddEventHandlers();
        services.AddPipeline(new PipelineBuilder<Scored>().CallsTestHandlers().Handlers(typeof(ScoredHandler)));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMessageHub>().SendAsync(new Scored());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(Log(provider), Is.EqualTo(new[] { "scored" }));
    }

    static ServiceProvider Services(Action<IServiceCollection> hubSteps) {
        var services = new ServiceCollection();
        var types = new AppTypes(new FixedModules(), typeof(HubStepTests).Assembly);
        services.AddSingleton(types);
        services.AddSingleton<HubLog>();
        services.AddCommandHandlers(types);
        services.AddRequestHandlers(types);
        services.AddEventHandlers();
        hubSteps(services);
        return services.BuildServiceProvider();
    }

    static ICommandSender Commands(IServiceProvider services) => services.GetRequiredService<ICommandSender>();

    static IReadOnlyList<string> Log(IServiceProvider services) => services.GetRequiredService<HubLog>().Lines;

    public sealed class HubLog {
        public List<string> Lines { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];
    }

    public sealed record Hit(int Points) : ICommand<int> {
        public const int Throws = -1;
        public const int Rejected = -2;
    }

    public sealed record Score : IRequest<ScoreResponse>;

    public sealed record ScoreResponse(int Points) : IResponse;

    public sealed record Scored : IntegrationEvent;

    public sealed record Unknown : IMessage;

    sealed class HitHandler(HubLog log) : CommandHandler<Hit, int> {
        public override Task<Result<int>> HandleAsync(IEnvelope<Hit> envelope) {
            log.Lines.Add($"hit {envelope.Message.Points}");
            return envelope.Message.Points switch {
                Hit.Throws => throw new InvalidOperationException("broken"),
                Hit.Rejected => Task.FromResult<Result<int>>(new Error("hit.rejected", "no")),
                _ => Task.FromResult<Result<int>>(envelope.Message.Points),
            };
        }
    }

    sealed class ScoreHandler(HubLog log, ICommandSender commands) : RequestHandler<Score, ScoreResponse> {
        public override async Task<Result<ScoreResponse>> HandleAsync(IEnvelope<Score> envelope) {
            log.Lines.Add("score");
            var hit = await commands.SendAsync(new Hit(5), default);
            return new ScoreResponse(hit.Value);
        }
    }

    sealed class ScoredHandler(HubLog log) : IntegrationEventHandler<Scored> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<Scored> envelope) {
            log.Lines.Add("scored");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    sealed class AuditHandler(HubLog log) : IIntegrationEventHandler<Scored> {
        public Task<Result<Unit>> HandleAsync(IEnvelope<Scored> envelope) {
            log.Lines.Add("audit");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    sealed class OuterStep(HubLog log) : HubStep {
        public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add($"outer before {message.GetType().Name}");
            return Task.FromResult<Error?>(null);
        }

        public override Task AfterAsync(object message, IMessageResult? result, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add($"outer after {(result is null ? "none" : result.Error?.Code ?? "ok")}");
            return Task.CompletedTask;
        }
    }

    sealed class InnerStep(HubLog log) : HubStep {
        public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add($"inner before {message.GetType().Name}");
            return Task.FromResult<Error?>(null);
        }

        public override Task AfterAsync(object message, IMessageResult? result, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add($"inner after {result?.Error?.Code ?? "ok"}");
            return Task.CompletedTask;
        }
    }

    sealed class TokenStep(HubLog log) : HubStep {
        public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) {
            log.Tokens.Add(ct);
            return Task.FromResult<Error?>(null);
        }

        public override Task AfterAsync(object message, IMessageResult? result, IServiceProvider services, CancellationToken ct) {
            log.Tokens.Add(ct);
            return Task.CompletedTask;
        }
    }

    sealed class BrokenAfterStep(HubLog log) : HubStep {
        public override Task AfterAsync(object message, IMessageResult? result, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add("broken after");
            throw new InvalidOperationException("broken after");
        }
    }

    sealed class BrokenBeforeStep(HubLog log) : HubStep {
        public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add("broken before");
            throw new InvalidOperationException("broken before");
        }
    }

    sealed class RefuseStep(HubLog log) : HubStep {
        public override Task<Error?> BeforeAsync(object message, IServiceProvider services, CancellationToken ct) {
            log.Lines.Add($"refuse {message.GetType().Name}");
            return Task.FromResult<Error?>(new Error("hub.closed", "The hub is closed."));
        }
    }
}
