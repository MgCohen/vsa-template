namespace Umpire.Vsa.Presenters;

using Umpire.Markers;
using Umpire.Messages.Contracts;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class Presenter(IBus bus) : IDisposable {
    IDisposable? live;

    public event Action? Changed;

    protected IBus Bus => bus;

    internal SemaphoreSlim Serial { get; } = new(1, 1);

    internal void RaiseChanged() => Changed?.Invoke();

    internal void Connect(IDisposable connection) {
        live?.Dispose();
        live = connection;
    }

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
