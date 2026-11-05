using Microsoft.Extensions.Logging;
using VSA.Contracts.Events;

namespace VSA.Presenters;

// The presenters that exist now, for the whole app. AddVsaPresenter adds each presenter when the services make it,
// and the scope removes it when it ends. A view is not needed.
// After each publish, each live presenter that implements IPresenterEvent<TEvent> gets the event one time.
// The events of one presenter run one at a time, on the dispatcher of its scope if the app gives one
// (a LivePresenters.Dispatch service), then the presenter raises Changed.
// The publisher does not wait for the views, and the token of the publisher does not go to the presenters:
// the handlers are done when the observer runs. An error in one presenter goes to the log.
public sealed class LivePresenters(ILogger<LivePresenters> logger) : IIntegrationEventObserver {
    readonly object gate = new();
    readonly Dictionary<Presenter, List<Dispatch>> live = [];

    // Runs work on a thread that the app selects, for example the main thread in Unity. With no dispatcher,
    // the work runs on the thread of the publisher.
    public delegate Task Dispatch(Func<Task> work);

    public IDisposable Add(Presenter presenter, Dispatch? dispatch = null) {
        var entry = dispatch ?? (work => work());
        lock (gate) {
            if (!live.TryGetValue(presenter, out var dispatchers)) {
                live[presenter] = dispatchers = [];
            }
            dispatchers.Add(entry);
        }
        return new Removal(this, presenter, entry);
    }

    // Starts the delivery to each live presenter and returns. The views get the event on their own dispatchers.
    public Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        (Presenter Presenter, Dispatch Dispatch)[] targets;
        lock (gate) {
            targets = [.. live.Where(p => p.Key is IPresenterEvent<TEvent>).Select(p => (p.Key, p.Value[0]))];
        }
        foreach (var (presenter, dispatch) in targets) {
            _ = Deliver((IPresenterEvent<TEvent>)presenter, presenter, dispatch, integrationEvent);
        }
        return Task.CompletedTask;
    }

    async Task Deliver<TEvent>(IPresenterEvent<TEvent> target, Presenter presenter, Dispatch dispatch, TEvent integrationEvent)
        where TEvent : IntegrationEvent {
        try {
            await dispatch(async () => {
                await presenter.Serial.WaitAsync();
                try {
                    await target.OnEventAsync(integrationEvent, CancellationToken.None);
                } finally {
                    presenter.Serial.Release();
                }
                presenter.RaiseChanged();
            });
        } catch (Exception e) {
            logger.LogError(e, "{Presenter} failed on {Event}.", presenter.GetType().Name, typeof(TEvent).Name);
        }
    }

    void Remove(Presenter presenter, Dispatch dispatch) {
        lock (gate) {
            if (live.TryGetValue(presenter, out var dispatchers) && dispatchers.Remove(dispatch) && dispatchers.Count == 0) {
                live.Remove(presenter);
            }
        }
    }

    sealed class Removal(LivePresenters owner, Presenter presenter, Dispatch dispatch) : IDisposable {
        int disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref disposed, 1) == 0) {
                owner.Remove(presenter, dispatch);
            }
        }
    }
}
