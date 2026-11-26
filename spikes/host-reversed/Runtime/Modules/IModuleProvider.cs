namespace Spike.Kernels;

public interface IModuleProvider {
    IReadOnlyList<Module> Modules { get; }

    IReadOnlyList<System.Reflection.Assembly> Assemblies { get; }
}
