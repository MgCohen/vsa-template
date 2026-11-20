namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

public sealed class BroadcastTests {
    [Test]
    public async Task A_throw_in_one_event_handler_does_not_stop_the_next_handlers() {
        using var services = Services(typeof(ThrowingPing), typeof(CountingPing));

        var result = await Publish(services, new Pinged());

        Assert.That(services.GetRequiredService<PingLog>().Lines, Is.EqualTo(new[] { "throwing", "counting" }));
        Assert.That(result.Error?.Message, Does.StartWith("ThrowingPing failed: broken"));
        Assert.That(result.Envelope?.HandlerExceptions.Single().StackTrace, Does.Contain(nameof(ThrowingPing)));
    }

    [Test]
    public async Task Each_broadcast_pipeline_runs_in_registration_order() {
        using var services = Services(
            [typeof(CountingPing)], new PipelineBuilder<IntegrationEvent>().Handlers(typeof(AuditPing)));

        var result = await Publish(services, new Pinged());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(services.GetRequiredService<PingLog>().Lines, Is.EqualTo(new[] { "counting", "audit" }));
    }

    [Test]
    public void A_cancelled_publish_stops_the_handlers() {
        using var services = Services(typeof(CancelledPing), typeof(CountingPing));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.That(() => Publish(services, new Pinged(), cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(services.GetRequiredService<PingLog>().Lines, Is.Empty);
    }

    static ServiceProvider Services(params Type[] handlers) => Services(handlers, null);

    static ServiceProvider Services(Type[] handlers, PipelineBuilder<IntegrationEvent>? second) {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes([]));
        services.AddSingleton<PingLog>();
        services.AddEventHandlers();
        services.AddPipeline(new PipelineBuilder<Pinged>().Handlers(handlers));
        if (second is not null) {
            services.AddPipeline(second);
        }
        return services.BuildServiceProvider();
    }

    static Task<Result<Unit>> Publish(IServiceProvider services, Pinged pinged, CancellationToken ct = default) =>
        services.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(pinged, ct);

    public sealed record Pinged : IntegrationEvent;

    public sealed class PingLog {
        public List<string> Lines { get; } = [];
    }

    sealed class ThrowingPing(PingLog log) : IntegrationEventHandler<Pinged> {
        public override Task<Result<Unit>> HandleAsync(Envelope<Pinged> envelope) {
            log.Lines.Add("throwing");
            throw new InvalidOperationException("broken");
        }
    }

    sealed class CountingPing(PingLog log) : IntegrationEventHandler<Pinged> {
        public override Task<Result<Unit>> HandleAsync(Envelope<Pinged> envelope) {
            log.Lines.Add("counting");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    sealed class CancelledPing(PingLog log) : IntegrationEventHandler<Pinged> {
        public override Task<Result<Unit>> HandleAsync(Envelope<Pinged> envelope) {
            envelope.Cancellation.ThrowIfCancellationRequested();
            log.Lines.Add("cancelled");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    sealed class AuditPing(PingLog log) : IIntegrationEventHandler<Pinged> {
        public Task<Result<Unit>> HandleAsync(Envelope<Pinged> envelope) {
            log.Lines.Add("audit");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}
