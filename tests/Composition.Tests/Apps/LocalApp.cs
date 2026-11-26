namespace Composition.Tests;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class LocalApp<THost>(params Module[] modules) : App<VsaKernel, THost>
    where THost : LocalHost, new() {
    public override AppHost CreateHost() => new THost { ModuleProvider = new FixedModules([.. modules]) };
}
