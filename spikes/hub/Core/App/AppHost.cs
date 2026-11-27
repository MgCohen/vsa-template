namespace Spike.Core;

public abstract class AppHost {
    public abstract Task<int> Run(IHub hub);
}
