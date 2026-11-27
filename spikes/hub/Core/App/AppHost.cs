namespace Spike.Core;

public abstract class AppHost {
    public abstract Task Open(IOperations operations, AppStop stop);

    public virtual ValueTask Close() => default;
}
