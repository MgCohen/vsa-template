namespace Umpire.Kernel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Umpire.Markers;

[RequireDerived]
public abstract class AppHost {
    IHost? built;

    public IServiceProvider? Services => built?.Services;

    protected IHost Built => built ?? throw new InvalidOperationException(
        $"{GetType().Name} has no built host. Call Create before Run or Stop.");

    public IHost Create(string[] args, App app) {
        var builder = CreateBuilder(args);
        Compose(builder.Services, app, builder.Configuration);
        built = Build(builder);
        return built;
    }

    public IServiceCollection Compose(IServiceCollection services, App app) =>
        Compose(services, app, ConfigurationIn(services) ?? new ConfigurationBuilder().Build());

    public virtual int Run() {
        Built.Run();
        return 0;
    }

    public virtual void Stop() =>
        Built.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

    protected virtual IHostApplicationBuilder CreateBuilder(string[] args) => Host.CreateApplicationBuilder(args);

    protected virtual void Install(IServiceCollection services, AppTypes types) {
    }

    protected virtual IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    static void InstallCore(IServiceCollection services, AppTypes types, IConfiguration configuration) {
        services.TryAddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton(types);
        AutoRegistration.Add(services, types);
    }

    static IConfiguration? ConfigurationIn(IServiceCollection services) =>
        services.LastOrDefault(service => service.ServiceType == typeof(IConfiguration)) switch {
            null => null,
            { ImplementationInstance: IConfiguration configuration } => configuration,
            _ => throw new InvalidOperationException(
                "The services register IConfiguration with a factory or a type, "
                + "so Compose cannot read it before the build. "
                + "Register the instance: services.AddSingleton<IConfiguration>(configuration)."),
        };

    IServiceCollection Compose(IServiceCollection services, App app, IConfiguration configuration) {
        var types = new AppTypes(app.Modules(), app.GetType().Assembly);
        InstallCore(services, types, configuration);
        Install(services, types);
        foreach (var module in types.Modules) {
            module.Compose(services, configuration);
        }
        app.ConfigureServices(services);
        return services;
    }
}
