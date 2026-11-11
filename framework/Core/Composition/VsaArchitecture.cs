using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Composition;

static class VsaArchitecture {
    internal static IReadOnlyList<VSA.Module.Module> Modules() => AppScan.Modules();

    internal static void InstallLayers(IServiceCollection services, ModuleTypes types) {
        PipelineInstaller.Install(services, types);
        EventHandlerInstaller.Install(services, types);
        CommandHandlerInstaller.Install(services, types);
        RequestHandlerInstaller.Install(services, types);
        services.AddScoped<IBus>(provider => new InProcessBus(provider));
        PresenterInstaller.Install(services, types);
    }

    internal static void ConfigureModules(IServiceCollection services, IConfiguration configuration, ModuleTypes types) {
        foreach (var module in types.Modules) {
            module.Configure(new ModuleBuilder(module.Name, services, configuration));
        }
    }
}
