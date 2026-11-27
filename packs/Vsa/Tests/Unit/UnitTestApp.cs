namespace Umpire.Vsa.Testing.Unit;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

sealed class UnitTestApp<TKernel>(IServiceCollection testServices, IModuleProvider modules)
    : App<TKernel, UnitTestHost>
    where TKernel : Kernel, new() {
    internal IServiceProvider Provider =>
        Services ?? throw new InvalidOperationException("The app of the test does not run.");

    public override AppHost CreateHost() => new UnitTestHost { TestServices = testServices, ModuleProvider = modules };
}
