namespace Umpire.Kernels;

using Umpire.Architecture;

public abstract class Pack<TArchitecture, TKernel, THost> : Pack
    where TArchitecture : Architecture, new()
    where TKernel : Kernel, new()
    where THost : AppHost, new() {
    readonly TArchitecture architecture = new();
    readonly TKernel kernel = new();

    public override Architecture Architecture => architecture;

    public override Kernel Kernel => kernel;

    public override AppHost CreateHost() => new THost();
}
