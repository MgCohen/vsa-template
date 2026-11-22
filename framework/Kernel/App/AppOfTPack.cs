namespace Umpire.Kernels;

public abstract class App<TPack> : App, IAppEntry<TPack> where TPack : Pack, new() {
    readonly TPack pack = new();

    public sealed override Pack Pack => pack;
}
