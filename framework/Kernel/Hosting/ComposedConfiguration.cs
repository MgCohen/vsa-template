namespace VSA.Kernel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

sealed record ComposedConfiguration(IConfiguration Configuration) {
    internal static IConfiguration Of(IServiceCollection services) =>
        services.LastOrDefault(service => service.ServiceType == typeof(ComposedConfiguration))?.ImplementationInstance
            is ComposedConfiguration composed
            ? composed.Configuration
            : InstanceIn(services) ?? new ConfigurationBuilder().Build();

    internal static IConfiguration? InstanceIn(IServiceCollection services) =>
        services.LastOrDefault(service => service.ServiceType == typeof(IConfiguration)) switch {
            null => null,
            { ImplementationInstance: IConfiguration configuration } => configuration,
            _ => throw new InvalidOperationException(
                "The services register IConfiguration with a factory or a type, so Compose cannot read it before the build. "
                + "Register the instance: services.AddSingleton<IConfiguration>(configuration)."),
        };
}
