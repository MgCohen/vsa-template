namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public sealed class Composition(Kernel kernel, IModuleProvider modules) {
    public void Install(IServiceCollection services) {
        var types = new AppTypes(modules);
        Core.Install(services, types);
        kernel.Install(services, types);
        foreach (var module in types.Modules) {
            module.Install(services);
        }
    }
}
