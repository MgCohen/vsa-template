namespace VSA.Kernel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

sealed record ComposedConfiguration(IConfiguration Configuration) {
    internal static IConfiguration Of(IServiceCollection services) =>
        Instance<ComposedConfiguration>(services)?.Configuration
            ?? Instance<IConfiguration>(services)
            ?? new ConfigurationBuilder().Build();

    static T? Instance<T>(IServiceCollection services) where T : class =>
        services.LastOrDefault(service => service.ServiceType == typeof(T))?.ImplementationInstance as T;
}
