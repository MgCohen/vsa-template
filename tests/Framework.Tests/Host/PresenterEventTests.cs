namespace Framework.Tests.Host;

using Cached;
using Gauges;
using Gauges.Domain.Shared;
using Gauges.Presenters;
using Gauges.Slices.ReadGauge;
using Microsoft.Extensions.DependencyInjection;
using Shelf;
using Shelf.Presenters;
using Signals;
using Signals.Presenters;
using Signals.Slices.Ring;
using Umpire.Markers;
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
    public void EachPresenterIsRegisteredOneTime() {
        var registrations = WebApps.OnTestServer(new SignalsModule(), new ShelfModule()).Registrations();

        Assert.That(registrations.Count(s => s.ServiceType == typeof(BellPresenter)), Is.EqualTo(1));
        Assert.That(registrations.Count(s => s.ServiceType == typeof(ShelfPresenter)), Is.EqualTo(1));
        Assert.That(registrations.Count(s => s.ServiceType == typeof(Presenter)), Is.EqualTo(2));
    }

    [Test]
    public void OnlyThePresentersOfTheModulesAreRegistered() {
        var registrations = WebApps.OnTestServer(new ShelfModule()).Registrations();

        Assert.That(registrations.Any(s => s.ServiceType.Assembly == typeof(OpenPresenter<>).Assembly), Is.False);
    }

    [Test]
    public void TheModuleScanDoesNotTakeAPresenterAsAModuleHandler() {
        var registrations = WebApps.OnTestServer(new SignalsModule()).Registrations();

        Assert.That(registrations.Where(s => s.ImplementationType == typeof(BellPresenter)).Select(s => s.ServiceType),
            Is.EqualTo(new[] { typeof(BellPresenter) }));
        Assert.That(registrations.Any(s => s.ServiceType == typeof(IIntegrationEventHandler<Rang>)), Is.False);
        Assert.That(registrations.Count(s => s.ServiceType == typeof(IntegrationEventHandler<Rang>)), Is.EqualTo(1));
    }

    [Test]
    public void AnOpenPresenterIsClosedForEachTypeArgument() {
        var registrations = WebApps.OnTestServer(new GaugesModule()).Registrations();

        Assert.That(registrations.Count(s => s.ServiceType == typeof(GaugePresenter<Fuel>)), Is.EqualTo(1));
        Assert.That(registrations.Count(s => s.ServiceType == typeof(GaugePresenter<Heat>)), Is.EqualTo(1));
    }

    [Test]
    public async Task EachClosedPresenterOfAnOpenPresenterGetsTheEvent() {
        using var app = await WebApps.OnTestServer(new GaugesModule()).Start();
        using var scope = LiveScope(app);

        await Publish(scope, new GaugeRead(7));

        Assert.That(scope.ServiceProvider.GetRequiredService<GaugePresenter<Fuel>>().Last, Is.EqualTo(7));
        Assert.That(scope.ServiceProvider.GetRequiredService<GaugePresenter<Heat>>().Last, Is.EqualTo(7));
    }

    [Test]
    public void APresenterWithALifetimeOtherThanScopedStopsTheStart() {
        var start = WebApps.OnTestServer(new CachedModule());

        Assert.That(() => start.Registrations(),
            Throws.InvalidOperationException.With.Message.Contains("Cached.Presenters.CachedPresenter")
                .And.Message.Contains("Singleton"));
    }

    [Test]
    public async Task APresenterGetsTheEventAfterItsHandlersWithNoView() {
        using var app = await Start();
        using var scope = LiveScope(app);
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Publish(scope, "door");

        Assert.That(app.Services.GetRequiredService<RingLog>().Bells, Is.EqualTo(new[] { "door" }));
        Assert.That(presenter.Last, Is.EqualTo("door"));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEventReachesThePresentersOfTwoLiveScopes() {
        using var app = await Start();
        using var first = LiveScope(app);
        using var second = LiveScope(app);
        var one = first.ServiceProvider.GetRequiredService<BellPresenter>();
        var two = second.ServiceProvider.GetRequiredService<BellPresenter>();

        using (var request = app.Services.CreateScope()) {
            await Publish(request, "door");
        }

        Assert.That(one.Last, Is.EqualTo("door"));
        Assert.That(two.Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task APresenterOfAScopeThatEndedGetsNothing() {
        using var app = await Start();
        BellPresenter presenter;
        using (var circuit = LiveScope(app)) {
            presenter = circuit.ServiceProvider.GetRequiredService<BellPresenter>();
        }
        using var request = app.Services.CreateScope();

        await Publish(request, "door");

        Assert.That(presenter.Last, Is.Null);
    }

    [Test]
    public async Task APresenterOfAScopeThatIsNotLiveGetsNothing() {
        using var app = await Start();
        using var scope = app.Services.CreateScope();
        var presenter = scope.ServiceProvider.GetRequiredService<BellPresenter>();

        await Publish(scope, "door");

        Assert.That(presenter.Last, Is.Null);
    }

    [Test]
    public async Task TheDeliveryMakesAPresenterThatDoesNotExistYetInALiveScope() {
        using var app = await Start();
        using var scope = LiveScope(app);

        await Publish(scope, "door");

        Assert.That(scope.ServiceProvider.GetRequiredService<BellPresenter>().Last, Is.EqualTo("door"));
    }

    [Test]
    public async Task APresenterDoesNotGetAnEventThatAHookRejects() {
        using var app = await Start();
        using var scope = LiveScope(app);

        await Publish(scope, "silent");

        Assert.That(scope.ServiceProvider.GetRequiredService<BellPresenter>().Last, Is.Null);
    }

    static IServiceScope LiveScope(WebRun app) {
        var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        return scope;
    }

    static Task Publish(IServiceScope scope, string bell) => Publish(scope, new Rang(bell));

    static Task Publish(IServiceScope scope, IntegrationEvent published) =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(published, CancellationToken.None);

    static Task<WebRun> Start() => WebApps.OnTestServer(new SignalsModule()).Start();

    sealed class OpenPresenter<T>() : Presenter(null!);

    [AutoClose]
    readonly record struct OpenPresenterArgument;
}
