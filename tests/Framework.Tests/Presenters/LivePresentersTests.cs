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
    public async Task APublishFromAHandlerInTheSameScopeRunsAfterTheCurrentEvent() {
        using var services = Services(s => {
            Add<PresenterWhoseRequestPublishes, Counted>(s);
            Add<OtherPresenter, Other>(s);
        });
        using var scope = LiveScope(services);

        var publish = Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);

        Assert.That(await Task.WhenAny(publish, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(publish));
        Assert.That(scope.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(1));
    }

    [Test]
    public async Task TwoScopesThatPublishToEachOtherDuringADeliveryDoNotWaitForEachOther() {
        using var services = Services(s => {
            s.AddSingleton<Meeting>();
            Add<KnockPresenter, Knock>(s);
            Add<OtherPresenter, Other>(s);
        });
        using var first = LiveScope(services);
        using var second = LiveScope(services);
        first.ServiceProvider.GetRequiredService<KnockPresenter>().Id = 1;
        second.ServiceProvider.GetRequiredService<KnockPresenter>().Id = 2;

        var meeting = services.GetRequiredService<Meeting>();

        var firstPublish = Task.Run(() => Live(services).OnPublishedAsync(new Knock(1), CancellationToken.None));
        await meeting.FirstArrived.Task;
        var secondPublish = Task.Run(() => Live(services).OnPublishedAsync(new Knock(2), CancellationToken.None));
        var both = Task.WhenAll(firstPublish, secondPublish);

        Assert.That(await Task.WhenAny(both, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(both));
        Assert.That(first.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(2));
        Assert.That(second.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(2));
    }

    [Test]
    public async Task APublishFromATaskThatAPresenterStartedWaitsForTheTurnOfTheScope() {
        using var services = Services(s => {
            s.AddScoped<Turns>();
            Add<SpawnPresenter, Counted>(s);
            Add<SlowOtherPresenter, Slow>(s);
            Add<TurnPresenter, Other>(s);
        });
        using var scope = LiveScope(services);
        var spawner = scope.ServiceProvider.GetRequiredService<SpawnPresenter>();
        var slow = scope.ServiceProvider.GetRequiredService<SlowOtherPresenter>();
        await Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);

        var slowPublish = Task.Run(() => Live(services).OnPublishedAsync(new Slow(), CancellationToken.None));
        await slow.Started.Task;
        spawner.Go.SetResult();
        await spawner.Spawned!;
        slow.Release();
        await slowPublish;

        Assert.That(scope.ServiceProvider.GetRequiredService<TurnPresenter>().Seen, Is.EqualTo(1));
        Assert.That(scope.ServiceProvider.GetRequiredService<Turns>().MostAtOnce, Is.EqualTo(1));
    }

    [Test]
    public async Task AScopeThatEndsDuringTheDeliveryGetsNoMoreHandlersAndNoError() {
        var errors = new ErrorLog();
        using var services = Services(s => {
            Add<EndingPresenter, Counted>(s);
            Add<CountPresenter, Counted>(s);
        }, errors);
        var scope = LiveScope(services);
        var ending = scope.ServiceProvider.GetRequiredService<EndingPresenter>();
        var counter = scope.ServiceProvider.GetRequiredService<CountPresenter>();

        var publish = Live(services).OnPublishedAsync(new Counted(1), CancellationToken.None);
        await ending.Started.Task;
        scope.Dispose();
        ending.Release();
        await publish;

        Assert.That(counter.Total, Is.Zero);
        Assert.That(errors.Count, Is.Zero);
    }

    [Test]
    public void APresenterScopeFromTheRootProviderThrows() {
        using var services = Services(Add<CountPresenter, Counted>);

        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<PresenterScope>());
    }

    static ServiceProvider Services(Action<IServiceCollection> presenters, ILogger<LivePresenters>? logger = null) {
        var services = new ServiceCollection();
        services.AddSingleton(logger ?? NullLogger<LivePresenters>.Instance);
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

    sealed record Knock(int Scope) : IntegrationEvent;

    sealed record Slow : IntegrationEvent;

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

    sealed class PresenterWhoseRequestPublishes(LivePresenters live) : Presenter(null!), IPresenterHandler<Counted> {
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

    sealed class Meeting {
        readonly TaskCompletionSource bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int arrived;

        public TaskCompletionSource FirstArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ArriveAsync() {
            if (Interlocked.Increment(ref arrived) == 2) {
                bothArrived.SetResult();
            }
            FirstArrived.TrySetResult();
            return bothArrived.Task;
        }
    }

    sealed class KnockPresenter(Meeting meeting, LivePresenters live) : Presenter(null!), IPresenterHandler<Knock> {
        public int Id { get; set; }

        public async Task<Result<Unit>> HandleAsync(Envelope<Knock> envelope) {
            if (envelope.Message.Scope != Id) {
                return Unit.Value;
            }
            await meeting.ArriveAsync();
            await live.OnPublishedAsync(new Other(), CancellationToken.None);
            return Unit.Value;
        }
    }

    sealed class Turns {
        int running;

        public int MostAtOnce { get; private set; }

        public void Enter() => MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref running));

        public void Leave() => Interlocked.Decrement(ref running);
    }

    sealed class SpawnPresenter(LivePresenters live) : Presenter(null!), IPresenterHandler<Counted> {
        public TaskCompletionSource Go { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? Spawned { get; private set; }

        public Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) {
            Spawned = Task.Run(async () => {
                await Go.Task;
                await live.OnPublishedAsync(new Other(), CancellationToken.None);
            });
            return Done;
        }
    }

    sealed class SlowOtherPresenter(Turns turns) : Presenter(null!), IPresenterHandler<Slow> {
        readonly SemaphoreSlim gate = new(0);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task<Result<Unit>> HandleAsync(Envelope<Slow> envelope) {
            turns.Enter();
            Started.TrySetResult();
            await gate.WaitAsync();
            turns.Leave();
            return Unit.Value;
        }
    }

    sealed class TurnPresenter(Turns turns) : Presenter(null!), IPresenterHandler<Other> {
        public int Seen { get; private set; }

        public Task<Result<Unit>> HandleAsync(Envelope<Other> envelope) {
            turns.Enter();
            Seen++;
            turns.Leave();
            return Done;
        }
    }

    sealed class EndingPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        readonly SemaphoreSlim gate = new(0);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task<Result<Unit>> HandleAsync(Envelope<Counted> envelope) {
            Started.TrySetResult();
            await gate.WaitAsync();
            envelope.Services.GetRequiredService<CountPresenter>();
            return Unit.Value;
        }
    }

    sealed class ErrorLog : ILogger<LivePresenters> {
        public int Count { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Error) {
                Count++;
            }
        }
    }
}
