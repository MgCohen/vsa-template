namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Signals;
using Signals.Slices.Ring;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

public sealed class EventHookTests {
    [Test]
    public async Task The_event_hooks_run_around_the_event_handler() {
        using var host = new TestApp(new SignalsModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("door"), default);

        var log = host.Services.GetRequiredService<RingLog>();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(log.Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(log.Hooks, Is.EqualTo(new[] { "chime:door:Accepted" }));
    }

    [Test]
    public async Task A_Before_event_hook_rejects_the_event_before_the_handler() {
        using var host = new TestApp(new SignalsModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new Rang("silent"), default);

        var log = host.Services.GetRequiredService<RingLog>();
        Assert.That(result.Error?.Code, Is.EqualTo("signals.silent"));
        Assert.That(result.Envelope?.State, Is.EqualTo(EnvelopeState.Rejected));
        Assert.That(log.Bells, Is.Empty);
        Assert.That(log.Hooks, Is.EqualTo(new[] { "chime:silent:Rejected" }));
    }
}
