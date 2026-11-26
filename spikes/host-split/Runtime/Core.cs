namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

static class Core {
    internal static void Install(IServiceCollection services, AppTypes types) {
        services.AddSingleton(types);
        services.AddSingleton<AppStop>();
    }
}
