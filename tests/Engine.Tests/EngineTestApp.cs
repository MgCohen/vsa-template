namespace Engine.Tests;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

sealed class EngineTestApp(params Module[] modules) : App {
    public override Kernel Kernel { get; } = new VsaKernel();

    public override AppHost CreateHost() => new() { ModuleProvider = new FixedModules(modules) };

    public EngineBuild Build() {
        var host = CreateHost();
        return new EngineBuild(host, host.Build([], this));
    }
}
