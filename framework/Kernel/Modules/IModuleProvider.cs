namespace Umpire.Kernels;

public interface IModuleProvider {
    IReadOnlyList<Module> Modules(ModuleScan scan);
}
