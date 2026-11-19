namespace Framework.Tests.Host;

using Microsoft.Extensions.DependencyInjection;
using Shelf;
using Shelf.Presenters;
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
    public void EachPresenterIsRegisteredOneTime() {
        var registrations = WebApps.OnTestServer(new SignalsModule(), new ShelfModule()).Registrations();

        Assert.That(registrations.Count(s => s.ServiceType == typeof(BellPresenter)), Is.EqualTo(1));
        Assert.That(registrations.Count(s => s.ServiceType == typeof(ShelfPresenter)), Is.EqualTo(1));
        Assert.That(registrations.Any(s => s.ServiceType == typeof(Presenter)), Is.False);
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
        Assert.That(registrations.Count(s => s.ServiceType == typeof(IPresenterHandler<Rang>)), Is.EqualTo(1));
        Assert.That(registrations.Count(s => s.ServiceType == typeof(IntegrationEventHandler<Rang>)), Is.EqualTo(1));
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

    static IServiceScope LiveScope(WebRun app) {
        var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        return scope;
    }

    static Task Publish(IServiceScope scope, string bell) =>
        scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Rang(bell), CancellationToken.None);

    static Task<WebRun> Start() => WebApps.OnTestServer(new SignalsModule()).Start();

    sealed class OpenPresenter<T>() : Presenter(null!);
}
