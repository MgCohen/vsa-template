namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

sealed class Core(HostArguments arguments) : Kernel {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.TryAddSingleton(arguments);
        services.AddSingleton(types);
        services.TryAddSingleton<AppStop>();
        AutoRegistration.Add(services, types);
    }
}
