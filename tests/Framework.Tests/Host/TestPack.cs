namespace Framework.Tests.Host;

using Umpire.Kernels;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Definition;

sealed class TestPack(Func<AppHost> create) : Pack {
    public override Umpire.Architecture.Architecture Architecture { get; } = new VsaArchitecture();

    public override Kernel Kernel { get; } = new VsaKernel();

    public override AppHost CreateHost() => create();
}
