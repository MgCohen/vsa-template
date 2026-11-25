namespace Umpire.Kernels;

public abstract class App<TKernel, THost> : App, IAppEntry<TKernel>
    where TKernel : Kernel, new()
    where THost : AppHost, new() {
    readonly TKernel kernel = new();

    public sealed override Kernel Kernel => kernel;

    public override AppHost CreateHost() => new THost();
}
