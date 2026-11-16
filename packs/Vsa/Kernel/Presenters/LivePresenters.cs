namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.Logging;
using Umpire.Vsa.Contracts.Events;

public sealed class LivePresenters(ILogger<LivePresenters> logger) : IIntegrationEventObserver {
    readonly object gate = new();
    readonly Dictionary<Presenter, List<Dispatch>> live = [];

    public delegate Task Dispatch(Func<Task> work);

    public void Connect(Presenter presenter, Dispatch? dispatch = null) {
        presenter.Connect(Add(presenter, dispatch));
    }

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
