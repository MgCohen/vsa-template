using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Contracts.Events;
using VSA.Handlers.Events;
using VSA.App;
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

    // Bottom up with no view: the services connect the presenter when they make it.
    [Test]
    public async Task APresenterGetsTheEventAfterItsHandlersWithNoView() {
        await using var app = await Start();
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(app.Services.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
        Assert.That(changed, Is.EqualTo(1));
    }

    // The event is published in another scope, for example a request. Each presenter that exists gets it.
    [Test]
    public async Task APresenterInAnotherScopeGetsTheEvent() {
        await using var app = await Start();
        using var circuit = app.Services.CreateScope();
        var presenter = circuit.ServiceProvider.GetRequiredService<BellPresenter>();

        using (var request = app.Services.CreateScope()) {
            await request.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);
        }

        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    // The end of the scope disposes the presenter, so it gets no more events.
    [Test]
    public async Task APresenterOfAScopeThatEndedGetsNothing() {
        await using var app = await Start();
        BellPresenter presenter;
        using (var circuit = app.Services.CreateScope()) {
            presenter = circuit.ServiceProvider.GetRequiredService<BellPresenter>();
        }
        using var request = app.Services.CreateScope();

        await request.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(presenter.Last, Is.Null);
    }

    // An app can give a dispatcher to the scope, for example the main thread in Unity.
    [Test]
    public async Task AScopeDispatcherRunsTheEvent() {
        var dispatched = 0;
        await using var app = await Start(services => services.AddScoped<LivePresenters.Dispatch>(_ => work => {
            dispatched++;
            return work();
        }));
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(1));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    static async Task<WebApplication> Start(Action<IServiceCollection>? configure = null) {
        var builder = WebApplication.CreateBuilder();
        // A real server on loopback: the presenter needs the address of the bus.
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddVsa(new SignalsModule());
        configure?.Invoke(builder.Services);
        var app = builder.Build();
        await app.StartAsync();
        return app;
    }
}
