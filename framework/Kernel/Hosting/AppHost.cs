namespace Umpire.Kernel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Umpire.Markers;

[RequireDerived]
public abstract class AppHost {
    public IHost Create(string[] args, App app) {
        var builder = CreateBuilder(args);
        Compose(builder.Services, app, builder.Configuration);
        return Build(builder);
    }

    public IServiceCollection Compose(IServiceCollection services, App app) =>
        Compose(services, app, ComposedConfiguration.InstanceIn(services));

    public virtual int Run(IHost app) {
        app.Run();
        return 0;
    }

    public virtual void Stop(IHost app) =>
        app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

    protected virtual IHostApplicationBuilder CreateBuilder(string[] args) => Host.CreateApplicationBuilder(args);

    protected virtual void Install(IServiceCollection services, AppTypes types) {
    }

    protected virtual IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    static void InstallCore(IServiceCollection services, AppTypes types, IConfiguration configuration) {
        services.TryAddSingleton(configuration);
        services.AddSingleton(new ComposedConfiguration(configuration));
        services.AddLogging();
        services.AddSingleton(types);
        AutoRegistration.Add(services, types);
    }

    IServiceCollection Compose(IServiceCollection services, App app, IConfiguration? configuration) {
        var types = new AppTypes(app.Modules() ?? AppTypes.FindModules(), app.Keys());
        InstallCore(services, types, configuration ?? new ConfigurationBuilder().Build());
        Install(services, types);
        foreach (var module in types.Modules) {
            module.Configure(services);
        }
        app.ConfigureServices(services);
        return services;
    }
}
