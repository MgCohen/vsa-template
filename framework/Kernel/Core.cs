namespace Umpire.Kernels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class Core(IConfiguration configuration) : Kernel {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.TryAddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton(types);
        services.TryAddSingleton<AppStop>();
        AutoRegistration.Add(services, types);
    }
}
