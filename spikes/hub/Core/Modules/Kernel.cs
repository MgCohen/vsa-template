namespace Spike.Core;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public class Kernel {
    public void Install(IServiceCollection services, Assembly app) => Configure(services, app);

    protected virtual void Configure(IServiceCollection services, Assembly app) {
    }
}
