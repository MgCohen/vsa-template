namespace Engine.Tests;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

sealed class EngineTestApp(params Module[] modules) : App {
    public override Kernel Kernel { get; } = new VsaKernel();

    public EngineBuild Build() {
        var host = new EngineTestHost { ModuleProvider = new FixedModules(modules) };
        return new EngineBuild(host, host.Build(this));
    }
}
