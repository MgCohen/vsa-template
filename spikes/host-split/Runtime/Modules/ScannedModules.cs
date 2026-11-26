namespace Spike.Kernels;

using System.Reflection;

public sealed class ScannedModules(Assembly assembly) : IModuleProvider {
    public IReadOnlyList<Module> Modules { get; } = [.. assembly.GetTypes()
        .Where(type => typeof(Module).IsAssignableFrom(type) && !type.IsAbstract)
        .Select(type => (Module)Activator.CreateInstance(type)!)];

    public IReadOnlyList<Assembly> Assemblies { get; } = [assembly];
}
