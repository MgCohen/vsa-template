namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Definition;
using VsaKernel = Umpire.Vsa.Composition.VsaKernel;

public sealed class TestPack<THost>(IReadOnlyList<Module> modules) : Pack where THost : AppHost, ITestHost, new() {
    public IReadOnlyList<Action<IServiceCollection>> Configure { get; set; } = [];

    public override Umpire.Architecture.Architecture Architecture { get; } = new VsaArchitecture();

    public override Kernel Kernel { get; } = new VsaKernel();

    public override AppHost CreateHost() {
        var host = new THost { ModuleProvider = new FixedModules([.. modules]) };
        host.Actions.AddRange(Configure);
        return host;
    }
}
