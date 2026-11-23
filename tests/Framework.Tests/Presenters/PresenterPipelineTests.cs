namespace Framework.Tests.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Presenters;

public sealed class PresenterPipelineTests {
    [Test]
    public async Task APresenterOfALiveScopeGetsItsEventAndRaisesChanged() {
        using var services = Services([typeof(CountPresenter)]);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Publish(services, new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(3));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task APresenterWithTwoEventsGetsEachOne() {
        using var services = Services([typeof(TwoEventsPresenter)]);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<TwoEventsPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Publish(services, new Counted(4), CancellationToken.None);
        await Publish(services, new Other(), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(4));
        Assert.That(presenter.Others, Is.EqualTo(1));
        Assert.That(changed, Is.EqualTo(2));
    }

    [Test]
    public async Task AFailedHandlerStillRaisesChanged() {
        using var services = Services([typeof(ErrorPresenter)]);
        using var scope = LiveScope(services);
        var changed = 0;
        scope.ServiceProvider.GetRequiredService<ErrorPresenter>().Changed += () => changed++;

        await Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEnvelopeHasTheServicesOfThePresenterScope() {
        using var services = Services([typeof(CountPresenter)]);
        using var scope = LiveScope(services);

        await Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(scope.ServiceProvider.GetRequiredService<CountPresenter>().Services, Is.SameAs(scope.ServiceProvider));
    }

    [Test]
    public async Task APresenterWithNoHandlerForTheEventGetsNothing() {
        using var services = Services([typeof(CountPresenter)]);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        var changed = 0;
        presenter.Changed += () => changed++;

        await Publish(services, new Other(), CancellationToken.None);

        Assert.That(changed, Is.Zero);
    }

    [Test]
    public async Task AScopeAfterItsEndGetsNothing() {
        using var services = Services([typeof(CountPresenter)]);
        var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<CountPresenter>();
        scope.Dispose();

        await Publish(services, new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.Zero);
    }

    [Test]
    public async Task AFailedHandlerDoesNotStopTheNextOne() {
        using var services = Services([typeof(BrokenPresenter), typeof(ErrorPresenter), typeof(CountPresenter)]);
        using var scope = LiveScope(services);

        await Publish(services, new Counted(2), CancellationToken.None);

        Assert.That(scope.ServiceProvider.GetRequiredService<CountPresenter>().Total, Is.EqualTo(2));
    }

    [Test]
    public async Task ThePublishWaitsForTheHandlers() {
        using var services = Services([typeof(SlowPresenter)]);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<SlowPresenter>();

        var publish = Publish(services, new Counted(1), CancellationToken.None);
        Assert.That(publish.IsCompleted, Is.False);
        presenter.Release();
        await publish;

        Assert.That(presenter.Handled, Is.EqualTo(1));
    }

    [Test]
    public async Task TheEventsOfOneScopeRunOneAtATime() {
        using var services = Services([typeof(SlowPresenter)]);
        using var scope = LiveScope(services);
        var presenter = scope.ServiceProvider.GetRequiredService<SlowPresenter>();

        var first = Task.Run(() => Publish(services, new Counted(1), CancellationToken.None));
        var second = Task.Run(() => Publish(services, new Counted(1), CancellationToken.None));
        await presenter.Started.Task;
        presenter.Release();
        presenter.Release();
        await Task.WhenAll(first, second);

        Assert.That(presenter.Handled, Is.EqualTo(2));
        Assert.That(presenter.MostAtOnce, Is.EqualTo(1));
    }

    [Test]
    public async Task APublishFromAHandlerInTheSameScopeRunsAfterTheCurrentEvent() {
        using var services = Services([typeof(PresenterWhoseRequestPublishes), typeof(OtherPresenter)]);
        using var scope = LiveScope(services);

        var publish = Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(await Task.WhenAny(publish, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(publish));
        Assert.That(scope.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(1));
    }

    [Test]
    public async Task TwoScopesThatPublishToEachOtherDuringADeliveryDoNotWaitForEachOther() {
        using var services = Services([typeof(KnockPresenter), typeof(OtherPresenter)], s => s.AddSingleton<Meeting>());
        using var first = LiveScope(services);
        using var second = LiveScope(services);
        first.ServiceProvider.GetRequiredService<KnockPresenter>().Id = 1;
        second.ServiceProvider.GetRequiredService<KnockPresenter>().Id = 2;

        var meeting = services.GetRequiredService<Meeting>();

        var firstPublish = Task.Run(() => Publish(services, new Knock(1), CancellationToken.None));
        await meeting.FirstArrived.Task;
        var secondPublish = Task.Run(() => Publish(services, new Knock(2), CancellationToken.None));
        var both = Task.WhenAll(firstPublish, secondPublish);

        Assert.That(await Task.WhenAny(both, Task.Delay(TimeSpan.FromSeconds(5))), Is.SameAs(both));
        Assert.That(first.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(2));
        Assert.That(second.ServiceProvider.GetRequiredService<OtherPresenter>().Seen, Is.EqualTo(2));
    }

    [Test]
    public async Task APublishFromATaskThatAPresenterStartedWaitsForTheTurnOfTheScope() {
        using var services = Services(
            [typeof(SpawnPresenter), typeof(SlowOtherPresenter), typeof(TurnPresenter)], s => s.AddScoped<Turns>());
        using var scope = LiveScope(services);
        var spawner = scope.ServiceProvider.GetRequiredService<SpawnPresenter>();
        var slow = scope.ServiceProvider.GetRequiredService<SlowOtherPresenter>();
        await Publish(services, new Counted(1), CancellationToken.None);

        var slowPublish = Task.Run(() => Publish(services, new Slow(), CancellationToken.None));
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
        using var services = Services([typeof(EndingPresenter), typeof(CountPresenter)], errors);
        var scope = LiveScope(services);
        var ending = scope.ServiceProvider.GetRequiredService<EndingPresenter>();
        var counter = scope.ServiceProvider.GetRequiredService<CountPresenter>();

        var publish = Publish(services, new Counted(1), CancellationToken.None);
        await ending.Started.Task;
        scope.Dispose();
        ending.Release();
        await publish;

        Assert.That(counter.Total, Is.Zero);
        Assert.That(errors.Count, Is.Zero);
    }

    [Test]
    public async Task AChangedListenerThatThrowsDoesNotStopTheQueueOrTheOtherScopes() {
        var errors = new ErrorLog();
        using var services = Services([typeof(SlowPresenter), typeof(CountPresenter)], errors);
        using var first = LiveScope(services);
        using var second = LiveScope(services);
        var slow = first.ServiceProvider.GetRequiredService<SlowPresenter>();
        slow.Changed += () => throw new InvalidOperationException("listener");
        second.ServiceProvider.GetRequiredService<SlowPresenter>().Release();
        second.ServiceProvider.GetRequiredService<SlowPresenter>().Release();

        var drainer = Task.Run(() => Publish(services, new Counted(1), CancellationToken.None));
        await slow.Started.Task;
        await Publish(services, new Counted(2), CancellationToken.None);
        slow.Release();
        slow.Release();
        await drainer;

        Assert.That(slow.Handled, Is.EqualTo(2));
        Assert.That(first.ServiceProvider.GetRequiredService<CountPresenter>().Total, Is.EqualTo(3));
        Assert.That(second.ServiceProvider.GetRequiredService<CountPresenter>().Total, Is.EqualTo(3));
        Assert.That(errors.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task TheLogHasTheExceptionOfAPresenterThatThrows() {
        var errors = new ErrorLog();
        using var services = Services([typeof(BrokenPresenter)], errors);
        using var scope = LiveScope(services);

        await Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(errors.Exceptions, Has.Count.EqualTo(1));
        Assert.That(errors.Exceptions[0], Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("broken"));
        Assert.That(errors.Exceptions[0].StackTrace, Does.Contain(nameof(BrokenPresenter)));
    }

    [Test]
    public async Task AHandlerThatThrowsRaisesNoChanged() {
        var errors = new ErrorLog();
        using var services = Services([typeof(BrokenPresenter)], errors);
        using var scope = LiveScope(services);
        var changed = 0;
        scope.ServiceProvider.GetRequiredService<BrokenPresenter>().Changed += () => changed++;

        await Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(changed, Is.Zero);
        Assert.That(errors.Exceptions.Select(e => e.Message), Is.EqualTo(new[] { "broken" }));
    }

    [Test]
    public async Task AHandlerWithAnErrorResultRaisesChanged() {
        using var services = Services([typeof(ErrorPresenter)]);
        using var scope = LiveScope(services);
        var changed = 0;
        scope.ServiceProvider.GetRequiredService<ErrorPresenter>().Changed += () => changed++;

        await Publish(services, new Counted(1), CancellationToken.None);

        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task ACancelledDeliveryRaisesNoChanged() {
        using var services = Services([typeof(CancelledPresenter)]);
        using var scope = LiveScope(services);
        var changed = 0;
        scope.ServiceProvider.GetRequiredService<CancelledPresenter>().Changed += () => changed++;
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Publish(services, new Counted(1), cancelled.Token);

        Assert.That(changed, Is.Zero);
    }

    [Test]
    public async Task ACancelledDeliveryIsNotAnError() {
        var errors = new ErrorLog();
        using var services = Services([typeof(CancelledPresenter)], errors);
        using var scope = LiveScope(services);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Publish(services, new Counted(1), cancelled.Token);

        Assert.That(errors.Count, Is.Zero);
    }

    [Test]
    public void APresenterScopeFromTheRootProviderThrows() {
        using var services = Services([typeof(CountPresenter)]);

        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<PresenterScope>());
    }

    static Task<Result<Unit>> Done => Task.FromResult<Result<Unit>>(Unit.Value);

    static ServiceProvider Services(Type[] presenters, ILogger<LiveScopesRunner>? logger = null) =>
        Services(presenters, _ => { }, logger);

    static ServiceProvider Services(
        Type[] presenters, Action<IServiceCollection> more, ILogger<LiveScopesRunner>? logger = null) {
        var services = new ServiceCollection();
        services.AddSingleton(new AppTypes(new FixedModules()));
        services.AddSingleton(logger ?? NullLogger<LiveScopesRunner>.Instance);
        services.AddEventHandlers();
        services.AddSingleton<LiveScopes>();
        services.AddScoped<PresenterScope>();
        foreach (var presenter in presenters) {
            services.AddScoped(presenter);
        }
        services.AddPipeline(new PipelineBuilder<IntegrationEvent>().Handlers(presenters).Runner<LiveScopesRunner>());
        more(services);
        return services.BuildServiceProvider();
    }

    static IServiceScope LiveScope(IServiceProvider services) {
        var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PresenterScope>();
        return scope;
    }

    static Task<Result<Unit>> Publish(IServiceProvider services, IntegrationEvent published, CancellationToken ct) =>
        services.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(published, ct);

    sealed record Counted(int By) : IntegrationEvent;

    sealed record Other : IntegrationEvent;

    sealed record Knock(int Scope) : IntegrationEvent;

    sealed record Slow : IntegrationEvent;
    sealed class CountPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public int Total { get; private set; }
        public IServiceProvider? Services { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            Total += envelope.Message.By;
            Services = envelope.Services;
            return Done;
        }
    }

    sealed class TwoEventsPresenter() : Presenter(null!), IPresenterHandler<Counted>, IPresenterHandler<Other> {
        public int Total { get; private set; }
        public int Others { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            Total += envelope.Message.By;
            return Done;
        }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Other> envelope) {
            Others++;
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

        public async Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref running));
            Started.TrySetResult();
            await gate.WaitAsync();
            Interlocked.Decrement(ref running);
            Handled++;
            return Unit.Value;
        }
    }

    sealed class BrokenPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) => throw new InvalidOperationException("broken");
    }

    sealed class ErrorPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) =>
            Task.FromResult<Result<Unit>>(new Error("broken", "The presenter failed."));
    }

    sealed class PresenterWhoseRequestPublishes(IIntegrationEventPublisher events)
        : Presenter(null!), IPresenterHandler<Counted> {
        public async Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            await events.PublishAsync(new Other(), CancellationToken.None);
            return Unit.Value;
        }
    }

    sealed class OtherPresenter() : Presenter(null!), IPresenterHandler<Other> {
        public int Seen { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Other> envelope) {
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

    sealed class KnockPresenter(Meeting meeting, IIntegrationEventPublisher events)
        : Presenter(null!), IPresenterHandler<Knock> {
        public int Id { get; set; }

        public async Task<Result<Unit>> HandleAsync(IEnvelope<Knock> envelope) {
            if (envelope.Message.Scope != Id) {
                return Unit.Value;
            }
            await meeting.ArriveAsync();
            await events.PublishAsync(new Other(), CancellationToken.None);
            return Unit.Value;
        }
    }

    sealed class Turns {
        int running;

        public int MostAtOnce { get; private set; }

        public void Enter() => MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref running));

        public void Leave() => Interlocked.Decrement(ref running);
    }

    sealed class SpawnPresenter(IIntegrationEventPublisher events) : Presenter(null!), IPresenterHandler<Counted> {
        public TaskCompletionSource Go { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? Spawned { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            Spawned = Task.Run(async () => {
                await Go.Task;
                await events.PublishAsync(new Other(), CancellationToken.None);
            });
            return Done;
        }
    }

    sealed class SlowOtherPresenter(Turns turns) : Presenter(null!), IPresenterHandler<Slow> {
        readonly SemaphoreSlim gate = new(0);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task<Result<Unit>> HandleAsync(IEnvelope<Slow> envelope) {
            turns.Enter();
            Started.TrySetResult();
            await gate.WaitAsync();
            turns.Leave();
            return Unit.Value;
        }
    }

    sealed class TurnPresenter(Turns turns) : Presenter(null!), IPresenterHandler<Other> {
        public int Seen { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Other> envelope) {
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

        public async Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            Started.TrySetResult();
            await gate.WaitAsync();
            envelope.Services.GetRequiredService<CountPresenter>();
            return Unit.Value;
        }
    }

    sealed class CancelledPresenter() : Presenter(null!), IPresenterHandler<Counted> {
        public Task<Result<Unit>> HandleAsync(IEnvelope<Counted> envelope) {
            envelope.Cancellation.ThrowIfCancellationRequested();
            return Done;
        }
    }

    sealed class ErrorLog : ILogger<LiveScopesRunner> {
        public int Count { get; private set; }
        public List<Exception> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Error) {
                Count++;
                if (exception is not null) {
                    Exceptions.Add(exception);
                }
            }
        }
    }
}
