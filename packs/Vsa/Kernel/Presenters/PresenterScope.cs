namespace Umpire.Vsa.Presenters;

// The first resolve in a scope adds that scope to LivePresenters. The end of the scope removes it.
public sealed class PresenterScope : IDisposable {
    readonly LivePresenters live;

    public PresenterScope(IServiceProvider services, LivePresenters live) {
        Services = services;
        this.live = live;
        live.Add(this);
    }

    internal IServiceProvider Services { get; }

    // Two publishes on two threads can reach the same scope at the same time,
    // for example two web requests and one Blazor circuit. The gate keeps the
    // presenters of a scope on one event at a time.
    internal SemaphoreSlim Gate { get; } = new(1, 1);

    public void Dispose() => live.Remove(this);
}
