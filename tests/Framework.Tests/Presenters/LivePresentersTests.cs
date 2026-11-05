using Microsoft.Extensions.Logging.Abstractions;
using VSA.Contracts.Events;
using VSA.Presenters;

namespace Framework.Tests.Presenters;

public sealed class LivePresentersTests {
    readonly LivePresenters live = new(NullLogger<LivePresenters>.Instance);

    [Test]
    public async Task ALivePresenterGetsItsEventAndRaisesChanged() {
        var presenter = new CountPresenter();
        var changed = 0;
        presenter.Changed += () => changed++;
        using var _ = live.Add(presenter);

        await live.OnPublishedAsync(new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(3));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public async Task APresenterWithNoInterfaceForTheEventGetsNothing() {
        var presenter = new CountPresenter();
        var changed = 0;
        presenter.Changed += () => changed++;
        using var _ = live.Add(presenter);

        await live.OnPublishedAsync(new Other(), CancellationToken.None);

        Assert.That(changed, Is.Zero);
    }

    [Test]
    public async Task ARemovedPresenterGetsNothing() {
        var presenter = new CountPresenter();
        live.Add(presenter).Dispose();

        await live.OnPublishedAsync(new Counted(3), CancellationToken.None);

        Assert.That(presenter.Total, Is.Zero);
    }

    // Two views that show the same presenter: the presenter gets each event one time, until the last view goes away.
    [Test]
    public async Task APresenterInTwoViewsGetsEachEventOneTime() {
        var presenter = new CountPresenter();
        var first = live.Add(presenter);
        using var second = live.Add(presenter);

        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);
        first.Dispose();
        first.Dispose();
        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(2));
    }

    [Test]
    public async Task AnErrorInOnePresenterDoesNotStopTheOthers() {
        var broken = new BrokenPresenter();
        var presenter = new CountPresenter();
        using var a = live.Add(broken);
        using var b = live.Add(presenter);

        await live.OnPublishedAsync(new Counted(2), CancellationToken.None);

        Assert.That(presenter.Total, Is.EqualTo(2));
    }

    [Test]
    public async Task TheEventRunsOnTheDispatcherOfTheView() {
        var presenter = new CountPresenter();
        var dispatched = 0;
        using var _ = live.Add(presenter, work => {
            dispatched++;
            return work();
        });

        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(1));
    }

    // The publisher does not wait for a view that is busy: the event waits on the dispatcher of that view.
    [Test]
    public async Task ThePublisherDoesNotWaitForABusyView() {
        var presenter = new CountPresenter();
        var view = new TaskCompletionSource();
        using var _ = live.Add(presenter, async work => {
            await view.Task;
            await work();
        });

        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);
        Assert.That(presenter.Total, Is.Zero);
        view.SetResult();

        Assert.That(presenter.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task ACanceledPublisherTokenDoesNotStopTheDelivery() {
        var presenter = new CountPresenter();
        using var _ = live.Add(presenter);

        await live.OnPublishedAsync(new Counted(1), new CancellationToken(canceled: true));

        Assert.That(presenter.Total, Is.EqualTo(1));
    }

    // Two events at the same time: the presenter handles one, then the other.
    [Test]
    public async Task TheEventsOfOnePresenterRunOneAtATime() {
        var presenter = new SlowPresenter();
        using var _ = live.Add(presenter);

        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);
        await live.OnPublishedAsync(new Counted(1), CancellationToken.None);
        Assert.That(presenter.Running, Is.EqualTo(1));
        presenter.Release();
        presenter.Release();
        await presenter.Done.Task;

        Assert.That(presenter.MostAtOnce, Is.EqualTo(1));
    }

    sealed record Counted(int By) : IntegrationEvent;

    sealed record Other : IntegrationEvent;

    sealed class CountPresenter() : Presenter(null!), IPresenterEvent<Counted> {
        public int Total { get; private set; }

        public Task OnEventAsync(Counted integrationEvent, CancellationToken ct) {
            Total += integrationEvent.By;
            return Task.CompletedTask;
        }
    }

    sealed class SlowPresenter() : Presenter(null!), IPresenterEvent<Counted> {
        readonly SemaphoreSlim gate = new(0);
        int handled;

        public int Running { get; private set; }
        public int MostAtOnce { get; private set; }
        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => gate.Release();

        public async Task OnEventAsync(Counted integrationEvent, CancellationToken ct) {
            Running++;
            MostAtOnce = Math.Max(MostAtOnce, Running);
            await gate.WaitAsync(ct);
            Running--;
            if (++handled == 2) {
                Done.SetResult();
            }
        }
    }

    sealed class BrokenPresenter() : Presenter(null!), IPresenterEvent<Counted> {
        public Task OnEventAsync(Counted integrationEvent, CancellationToken ct) => throw new InvalidOperationException("broken");
    }
}
