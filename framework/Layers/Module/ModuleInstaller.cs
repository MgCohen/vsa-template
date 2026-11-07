namespace VSA.Module;

public static class ModuleInstaller {
    public static void Install(VsaContext context) {
        foreach (var module in context.Modules) {
            module.Configure(new ModuleBuilder(module.Name, context.Services, context.Configuration));
        }
    }
}
