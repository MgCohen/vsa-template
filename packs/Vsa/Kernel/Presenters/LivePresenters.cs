namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.Logging;
using Umpire.Vsa.Contracts.Events;

public sealed class LivePresenters(ILogger<LivePresenters> logger) : IIntegrationEventObserver {
    readonly object gate = new();
    readonly Dictionary<Presenter, Live> live = [];

    public delegate Task Dispatch(Func<Task> work);

    public void Connect(Presenter presenter, Dispatch? dispatch = null) {
        var entry = new Live(dispatch ?? (work => work()), new SemaphoreSlim(1, 1));
        lock (gate) {
            live[presenter] = entry;
        }
        presenter.Connect(() => Disconnect(presenter, entry));
    }

    public Task OnPublishedAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        KeyValuePair<Presenter, Live>[] targets;
        lock (gate) {
            targets = [.. live.Where(p => p.Key is IPresenterEvent<TEvent>)];
        }
        foreach (var target in targets) {
            _ = Deliver(target.Key, target.Value, integrationEvent);
        }
        return Task.CompletedTask;
    }

    async Task Deliver<TEvent>(Presenter presenter, Live entry, TEvent integrationEvent) where TEvent : IntegrationEvent {
        try {
            await entry.Dispatch(async () => {
                await entry.Gate.WaitAsync();
                try {
                    await ((IPresenterEvent<TEvent>)presenter).OnEventAsync(integrationEvent, CancellationToken.None);
                } finally {
                    entry.Gate.Release();
                }
                presenter.RaiseChanged();
            });
        } catch (Exception e) {
            logger.LogError(e, "{Presenter} failed on {Event}.", presenter.GetType().Name, typeof(TEvent).Name);
        }
    }

    void Disconnect(Presenter presenter, Live entry) {
        lock (gate) {
            if (live.TryGetValue(presenter, out var current) && current == entry) {
                live.Remove(presenter);
            }
        }
    }

    sealed record Live(Dispatch Dispatch, SemaphoreSlim Gate);
}
