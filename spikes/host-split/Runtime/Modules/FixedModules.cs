namespace Spike.Kernels;

using System.Reflection;

public sealed class FixedModules(params Module[] modules) : IModuleProvider {
    public IReadOnlyList<Module> Modules { get; } = modules;

    public IReadOnlyList<Assembly> Assemblies { get; } = [.. modules.Select(module => module.GetType().Assembly).Distinct()];
}
