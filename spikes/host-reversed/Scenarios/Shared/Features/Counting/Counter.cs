namespace Greeter.Shared.Features.Counting;

public sealed class Counter : IDisposable {
    int count;
    bool disposed;

    public int Next() {
        ObjectDisposedException.ThrowIf(disposed, this);
        return Interlocked.Increment(ref count);
    }

    public void Dispose() => disposed = true;
}
