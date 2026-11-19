namespace Umpire.Vsa.Presenters;

public sealed class PresenterScope : IDisposable {
    readonly LivePresenters live;
    readonly Queue<Func<Task>> waitingDeliveries = new();
    bool delivering;

    public PresenterScope(IServiceProvider services, LivePresenters live) {
        if (ReferenceEquals(services, live.RootServices)) {
            throw new InvalidOperationException(
                "PresenterScope was resolved from the root provider, so it stays live until the app stops. "
                + "Resolve it in a scope (IServiceScopeFactory.CreateScope), and dispose that scope at the end of the view.");
        }
        Services = services;
        this.live = live;
        live.Add(this);
    }

    internal IServiceProvider Services { get; }

    internal async Task DeliverInTurnAsync(Func<Task> delivery) {
        lock (waitingDeliveries) {
            waitingDeliveries.Enqueue(delivery);
            if (delivering) {
                return;
            }
            delivering = true;
        }
        try {
            while (NextDelivery() is { } next) {
                await next();
            }
        } catch {
            lock (waitingDeliveries) {
                delivering = false;
            }
            throw;
        }
    }

    public void Dispose() => live.Remove(this);

    Func<Task>? NextDelivery() {
        lock (waitingDeliveries) {
            if (waitingDeliveries.Count > 0) {
                return waitingDeliveries.Dequeue();
            }
            delivering = false;
            return null;
        }
    }
}
