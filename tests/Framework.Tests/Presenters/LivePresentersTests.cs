namespace Framework.Tests.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Presenters;

public sealed class LivePresentersTests {
    [Test]
    public async Task APresenterOfALiveScopeGetsItsEventAndRaisesChanged() {
        using var services = Services(Add<CountPresenter, Counted>);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Live(services).OnPublishedAsync(new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(3));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEnvelopeHasTheServicesOfThePresenterScope() {
        using var services = Services(Add<CountPresenter, Counted>);
        using var scope = LiveScope(services);

        await Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);

        Assert.That(scope.ServiceProvider.GetRequiredService<CountPresenter>().Services, Is.SameAs(scope.ServiceProvider));
    }

    [Test]
    public async Task APresenterWithNoHandlerForTheEventGetsNothing() {
        using var services = Services(Add<CountPresenter, Counted>);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Live(services).OnPublishedAsync(new Other(), CancellationToken.None);

        Assert.That(changed, Is.Zero);
    }

    [Test]
    public async Task AScopeAfterItsEndGetsNothing() {
        using var services = Services(Add<CountPresenter, Counted>);
        var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        scope.Dispose();

        await Live(services).OnPublishedAsync(new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.Zero);
    }

    [Test]
    public async Task AFailedHandlerDoesNotStopTheNextOne() {
        using var services = Services(s => {
            Add<BrokenPresenter, Counted>(s);
            Add<ErrorPresenter, Counted>(s);
            Add<CountPresenter, Counted>(s);
        });
        using var scope = LiveScope(services);

        await Live(services).OnPublishedAsync(new Counted(2), CancellationToken.None);

        Assert.That(scope.ServiceProvider.GetRequiredService<CountPresenter>().Total, Is.EqualTo(2));
    }

    [Test]
    public async Task ThePublishWaitsForTheHandlers() {
        using var services = Services(Add<SlowPresenter, Counted>);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<SlowPresenter>();

        var publish = Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);
        Assert.That(publish.IsCompleted, Is.False);
        presenter.Release();
        await publish;

        Assert.That(presenter.Handled, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEventsOfOneScopeRunOneAtATime() {
        using var services = Services(Add<SlowPresenter, Counted>);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<SlowPresenter>();

        var first = Task.Run(() => Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None));
        var second = Task.Run(() => Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None));
        await presenter.Started.Task;
        presenter.Release();
        presenter.Release();
        await Task.WhenAll(first, second);

        Assert.That(presenter.Handled, Is.EqualTo(2));
        Assert.That(presenter.MostAtOnce, Is.EqualTo(1));
    }

    [Test]
    public async Task ASecondPublishFromAHandlerInTheSameScopeDoesNotWaitForTheGate() {
        using var services = Services(s => {
            Add<EchoPresenter, Counted>(s);
            Add<OtherPresenter, Other>(s);
        });
        using var scope = LiveScope(services);

        var publish = Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);

        Assert.That(await Task.WhenAny(publish, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(publish));
        Assert.That(scope.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(1));
    }

    static ServiceProvider Services(Action<IServiceCollection> presenters) {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<LivePresenters>>(NullLogger<LivePresenters>.Instance);
        services.AddSingleton<LivePresenters>();
        services.AddScoped<PresenterScope>();
        presenters(services);
        return services.BuildServiceProvider();
    }

    static void Add<TPresenter, TEvent>(IServiceCollection services)
        where TPresenter : Presenter, IPresenterHandler<TEvent> where TEvent : IntegrationEvent {
        services.AddScoped<TPresenter>();
        services.AddScoped<IPresenterHandler<TEvent>>(provider => provider.GetRequiredService<TPresenter>());
    }

    static IServiceScope LiveScope(IServiceProvider services) {
        var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        return scope;
    }

    static LivePresenters Live(IServiceProvider services) => services.GetRequiredService<LivePresenters>();

    static Task<Result<Unit>> Done => Task.FromResult<Result<Unit>>(Unit.Value);

    sealed record Counted(int By) : IntegrationEvent;

    sealed record Other : IntegrationEvent;

    sealed class CountPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public int Total { get; private set; }
        public IServiceProvider? Services { get; private set; }

        public Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) {
            Total += envelope.Message.By;
            Services = envelope.Services;
            return Done;
        }
    }

    sealed class SlowPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        readonly SemaphoreSlim gate = new(0);
        int running;

        public int Handled { get; private set; }
        public int MostAtOnce { get; private set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) {
            MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref running));
            Started.TrySetResult();
            await gate.WaitAsync();
            Interlocked.Decrement(ref running);
            Handled++;
            return Unit.Value;
        }
    }

    sealed class BrokenPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) => throw new InvalidOperationException("broken");
    }

    sealed class ErrorPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) =>
            Task.FromResult<Result<Unit>>(new Error("broken", "The presenter failed."));
    }

    // Stands for a presenter that sends a request, and the request handler publishes an event.
    sealed class EchoPresenter(LivePresenters live) : Presenter(null!), IPresenterHandler<Counted> {
        public async Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) {
            await live.OnPublishedAsync(new Other(), CancellationToken.None);
            return Unit.Value;
        }
    }

    sealed class OtherPresenter() : Presenter(null!), IPresenterHandler<Other> {
        public int Seen { get; private set; }

        public Task<Result<Unit>> HandleAsync(Envelope<Other> envelope) {
            Seen++;
            return Done;
        }
    }
}
