namespace Umpire.Kernels;

public sealed class ScannedModules : IModuleProvider {
    public IReadOnlyList<Module> Modules(AppTypes types) => types.ScannedModules();
}
