namespace Spike.Kernels;

public abstract class AppHost : IDisposable {
    public abstract void Open(IOperations operations, AppStop stop);

    public virtual void Dispose() => GC.SuppressFinalize(this);
}
