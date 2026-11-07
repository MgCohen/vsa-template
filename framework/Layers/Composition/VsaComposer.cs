using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
#if NET
using VSA.Presenters;
#endif

namespace VSA.Composition;

public sealed class VsaComposer(
    IServiceCollection services, IConfiguration configuration, IReadOnlyList<VSA.Module.Module>? modules = null) {
    public VsaContext Context { get; } = new(services, configuration, modules ?? DiscoveredModules());

    public void Install() {
        EventHandlerInstaller.Install(Context);
        CommandHandlerInstaller.Install(Context);
#if NET
        PresenterInstaller.Install(Context);
#endif
        ModuleInstaller.Install(Context);
    }

    static VSA.Module.Module[] DiscoveredModules() =>
        [.. ModuleDiscovery.CreateModules(ModuleDiscovery.FindRoots())];
}
