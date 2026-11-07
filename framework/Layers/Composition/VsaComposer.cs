using Microsoft.Extensions.Configuration;
using VSA.Module;

namespace VSA.Composition;

public sealed class VsaComposer {
    public VsaComposer(IVsaServices services, IConfiguration configuration)
        : this(services, configuration, DiscoveredModules()) { }

    public VsaComposer(IVsaServices services, IConfiguration configuration, params VSA.Module.Module[] modules) {
        Context = new VsaContext(services, configuration, modules);
    }

    public VsaContext Context { get; }

    public List<IVsaInstaller> Installers { get; } = DefaultInstallers();

    public static List<IVsaInstaller> DefaultInstallers() => [
        new CoreInstaller(),
        new EventHandlerInstaller(),
        new CommandHandlerInstaller(),
#if NET
        new PresenterInstaller(),
#endif
        new ModuleInstaller(),
    ];

    public static VSA.Module.Module[] DiscoveredModules() =>
        [.. ModuleDiscovery.CreateModules(ModuleDiscovery.FindRoots())];

    public VsaComposer AddBefore<TInstaller>(IVsaInstaller installer) where TInstaller : IVsaInstaller {
        Installers.Insert(IndexOf<TInstaller>(), installer);
        return this;
    }

    public VsaComposer Replace<TInstaller>(IVsaInstaller installer) where TInstaller : IVsaInstaller {
        Installers[IndexOf<TInstaller>()] = installer;
        return this;
    }

    public VsaComposer Remove<TInstaller>() where TInstaller : IVsaInstaller {
        Installers.RemoveAt(IndexOf<TInstaller>());
        return this;
    }

    public void Install() {
        foreach (var installer in Installers) {
            installer.Install(Context);
        }
    }

    int IndexOf<TInstaller>() where TInstaller : IVsaInstaller {
        var index = Installers.FindIndex(installer => installer is TInstaller);
        return index >= 0 ? index
            : throw new InvalidOperationException($"The composer has no {typeof(TInstaller).Name}.");
    }
}
