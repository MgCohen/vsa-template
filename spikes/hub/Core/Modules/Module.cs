namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

public abstract class Module {
    public void Install(IServiceCollection services) => Configure(services);

    protected virtual void Configure(IServiceCollection services) {
    }
}
