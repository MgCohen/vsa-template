namespace Umpire.Kernels;

public sealed class ScannedModules : IModuleProvider {
    public IReadOnlyList<Module> Modules(ModuleScan scan) => scan.All();
}
