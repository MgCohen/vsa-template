namespace Engine.Tests;

using Umpire.Kernels;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Definition;

sealed class EngineTestApp : App {
    public EngineTestApp(params Module[] modules) =>
        Pack = new EnginePack(new AppHost { ModuleProvider = new FixedModules(modules) });

    public override Pack Pack { get; }

    public EngineBuild Build() {
        var host = Pack.CreateHost();
        return new EngineBuild(host, host.Build([], this));
    }

    sealed class EnginePack(AppHost host) : Pack {
        public override Umpire.Architecture.Architecture Architecture { get; } = new VsaArchitecture();

        public override Kernel Kernel { get; } = new VsaKernel();

        public override AppHost CreateHost() => host;
    }
}
