namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public class Kernel {
    public void Install(IServiceCollection services, AppTypes types) => Configure(services, types);

    protected virtual void Configure(IServiceCollection services, AppTypes types) {
    }
}
