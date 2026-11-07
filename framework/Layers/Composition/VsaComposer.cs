using Microsoft.Extensions.Configuration;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
#if NET
using VSA.Presenters;
#endif

namespace VSA.Composition;

public sealed class VsaComposer {
    public VsaComposer(IVsaServices services, IConfiguration configuration)
        : this(services, configuration, DiscoveredModules()) { }

    public VsaComposer(IVsaServices services, IConfiguration configuration, params VSA.Module.Module[] modules) {
        Context = new VsaContext(services, configuration, modules);
    }

    public VsaContext Context { get; }

    public static VSA.Module.Module[] DiscoveredModules() =>
        [.. ModuleDiscovery.CreateModules(ModuleDiscovery.FindRoots())];

    public void Install() {
        InstallLayers();
        ConfigureModules();
    }

    public void InstallLayers() {
        new EventHandlerInstaller().Install(Context);
        new CommandHandlerInstaller().Install(Context);
#if NET
        new PresenterInstaller().Install(Context);
#endif
    }

    public void ConfigureModules() => new ModuleInstaller().Install(Context);
}
