namespace Umpire.Vsa.Presenters;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

[RequireDerived]
[Preserve]
public abstract class Presenter(IRequestSender requests) : IDisposable {
    Action? disconnect;

    public event Action? Changed;

    protected IRequestSender Requests => requests;

    internal void RaiseChanged() => Changed?.Invoke();

    internal void Connect(Action disconnectFromLive) {
        disconnect?.Invoke();
        disconnect = disconnectFromLive;
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
            disconnect?.Invoke();
            disconnect = null;
        }
    }
}
