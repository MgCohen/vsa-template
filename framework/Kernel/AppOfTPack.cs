namespace VSA.Kernel;

public abstract class App<TPack> : App where TPack : Pack, new() {
    readonly TPack pack = new();

    public sealed override Pack Pack => pack;
}
