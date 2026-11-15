namespace Engine.Tests;

using Microsoft.Extensions.Hosting;
using VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Kernel;

sealed class EngineTestApp(params Module[] modules) : App<Vsa<VsaHost>> {
    public IHost Build() => Pack.CreateHost().Create([], this);

    protected override IReadOnlyList<Module> Modules() => modules;
}
