using VSA.Contracts;

namespace VSA.Presenters;

// The interface between the views and one module. Two directions:
//   top down:  a view calls a method, the presenter sends a request with IBus (Run).
//   bottom up: an event of the module reaches the presenter (IPresenterEvent), and the presenter raises Changed.
// The presenter gets its events by itself: the services connect it to LivePresenters when they make it
// (PresenterInstaller), and its scope disposes it. A view only listens to Changed and renders.
public abstract class Presenter(IBus bus) : IDisposable {
    IDisposable? live;

    public event Action? Changed;

    protected IBus Bus => bus;

    // The events of one presenter run one at a time (LivePresenters).
    internal SemaphoreSlim Serial { get; } = new(1, 1);

    // LivePresenters calls it after a presenter event.
    internal void RaiseChanged() => Changed?.Invoke();

    // PresenterInstaller calls it when the services make the presenter.
    internal void Connect(IDisposable connection) {
        live?.Dispose();
        live = connection;
    }

    // Sets Busy on op, runs body, puts the message of the Error that body gives on op, and raises Changed.
    protected async Task Run(OpState op, Func<Task<Error?>> body) {
        op.Busy = true;
        op.Error = null;
        Changed?.Invoke();
        try {
            op.Error = (await body())?.Message;
        } finally {
            op.Busy = false;
            Changed?.Invoke();
        }
    }

    // The end of the scope: the presenter gets no more events.
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            live?.Dispose();
            live = null;
        }
    }
}
