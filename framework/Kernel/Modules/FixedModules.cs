namespace Umpire.Kernels;

public sealed class FixedModules(params Module[] modules) : IModuleProvider {
    public IReadOnlyList<Module> Modules(AppTypes types) => modules;
}
