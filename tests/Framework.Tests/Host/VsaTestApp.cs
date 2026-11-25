namespace Framework.Tests.Host;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

abstract class VsaTestApp(Func<AppHost> create) : App {
    public override Kernel Kernel { get; } = new VsaKernel();

    public override AppHost CreateHost() => create();
}
