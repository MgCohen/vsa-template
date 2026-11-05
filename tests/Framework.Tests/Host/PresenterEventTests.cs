using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Contracts.Events;
using VSA.Handlers.Events;
using VSA.Host;
using VSA.Presenters;

namespace Framework.Tests.Host;

// AddVsa with the Signals fixture: an event, its handler, a helper class next to the handler, and a presenter of the event.
public sealed class PresenterEventTests {
    [Test]
    public void HostRegistersTheEventHandlerAndNotTheHelperClass() {
        var builder = WebApplication.CreateBuilder();

        builder.AddVsa(new SignalsModule());

        Assert.That(builder.Services.Any(s => s.ServiceType == typeof(IntegrationEventHandler<Rang>)), Is.True);
        Assert.That(builder.Services.Any(s => s.ServiceType != typeof(RingLog) && s.ImplementationType == typeof(RingLog)), Is.False);
    }

    [Test]
    public async Task ALivePresenterGetsTheEventAfterItsHandlers() {
        await using var app = await Start();
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;
        using var _ = app.Services.GetRequiredService<LivePresenters>().Add(presenter);

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(app.Services.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task APresenterThatNoViewShowsGetsNothing() {
        await using var app = await Start();
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(presenter.Last, Is.Null);
    }

    static async Task<WebApplication> Start() {
        var builder = WebApplication.CreateBuilder();
        // A real server on loopback: the presenter needs the address of the bus.
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddVsa(new SignalsModule());
        var app = builder.Build();
        await app.StartAsync();
        return app;
    }
}
