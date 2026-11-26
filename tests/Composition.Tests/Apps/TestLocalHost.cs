namespace Composition.Tests;

using Umpire.Kernels;

public sealed class TestLocalHost : LocalHost {
    protected override AppTypes TypesOf(App app) => new(ModuleProvider);
}
