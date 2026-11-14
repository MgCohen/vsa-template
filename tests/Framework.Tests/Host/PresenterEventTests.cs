using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using VSA.Contracts.Events;
using VSA.Handlers.Events;
using VSA.Presenters;
using VSA.Kernel;

namespace Framework.Tests.Host;

// AppBuilder with the Signals fixture: an event, its handler, a helper class next to the handler, and a presenter of the event.
public sealed class PresenterEventTests {
    [Test]
    public void HostRegistersTheEventHandlerAndNotTheHelperClass() {
        var registrations = WebApps.OnTestServer(new SignalsModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType == typeof(IntegrationEventHandler<Rang>)), Is.True);
        Assert.That(registrations.Any(s => s.ServiceType != typeof(RingLog) && s.ImplementationType == typeof(RingLog)), Is.False);
    }

    // Bottom up with no view: the services connect the presenter when they make it.
    [Test]
    public async Task APresenterGetsTheEventAfterItsHandlersWithNoView() {
        using var app = await Start();
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
        using var app = await Start();
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
        using var app = await Start();
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
        using var app = await Start(services => services.AddScoped<LivePresenters.Dispatch>(_ => work => {
            dispatched++;
            return work();
        }));
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang("door"), CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(1));
        Assert.That(presenter.Last, Is.EqualTo("door"));
    }

    static Task<IHost> Start(Action<IServiceCollection>? configure = null) =>
        WebApps.OnTestServer(new SignalsModule()).Services(configure ?? (_ => { })).Start();
}
