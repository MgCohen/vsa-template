namespace Engine.Tests;

using Microsoft.Extensions.Hosting;
using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

sealed class EngineTestApp(params Module[] modules) : App<Vsa<VsaHost>> {
    public IHost Build() => Pack.CreateHost().Create([], this);

    protected override IReadOnlyList<Module> Modules() => modules;
}
