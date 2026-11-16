namespace Framework.Tests.Host;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Presenters;

public sealed class PresenterEventTests {
    [Test]
    public void HostRegistersTheEventHandlerAndNotTheHelperClass() {
        var registrations = WebApps.OnTestServer(new SignalsModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType == typeof(IntegrationEventHandler<Rang>)), Is.True);
        Assert.That(registrations.Any(s => s.ServiceType != typeof(RingLog) && s.ImplementationType == typeof(RingLog)), Is.False);
    }

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
        WebApps.OnTestServer(new SignalsModule()).With(configure ?? (_ => { })).Start();
}
