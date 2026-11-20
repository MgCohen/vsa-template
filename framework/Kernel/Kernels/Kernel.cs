namespace Umpire.Kernels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class Kernel {
    public void Install(IServiceCollection services, AppTypes types, IConfiguration configuration) {
        services.TryAddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton(types);
        services.TryAddSingleton<AppStop>();
        AutoRegistration.Add(services, types);
        Configure(services, types);
    }

    protected virtual void Configure(IServiceCollection services, AppTypes types) {
    }
}
