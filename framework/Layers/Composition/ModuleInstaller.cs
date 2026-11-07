using VSA.Module;

namespace VSA.Composition;

public sealed class ModuleInstaller : IVsaInstaller {
    public void Install(VsaContext context) {
        foreach (var module in context.Modules) {
            module.Configure(new ModuleBuilder(module.Name, context.Services, context.Configuration));
        }
    }
}
