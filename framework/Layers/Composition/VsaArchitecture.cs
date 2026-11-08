using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
#if NET
using VSA.Presenters;
#endif

namespace VSA.Composition;

static class VsaArchitecture {
    internal static IReadOnlyList<VSA.Module.Module> Modules() => AppScan.Modules();

    internal static void InstallLayers(IServiceCollection services, ModuleTypes types) {
        EventHandlerInstaller.Install(services, types);
        CommandHandlerInstaller.Install(services, types);
#if NET
        PresenterInstaller.Install(services, types);
#endif
    }

    internal static void ConfigureModules(IHostApplicationBuilder builder, ModuleTypes types) {
        foreach (var module in types.Modules) {
            module.Configure(new ModuleBuilder(module.Name, builder.Services, builder.Configuration));
        }
    }
}
