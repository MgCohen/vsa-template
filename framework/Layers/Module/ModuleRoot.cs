using System.Reflection;

namespace VSA.Module;

// A module root assembly and the records that VSA.Sdk wrote in it at build time.
// Name is the assembly name, the same as Module.Name. Layers are the assembly names of the layer projects
// of the module, for example "Notes.Handlers.Requests". A layer that the module does not have is not in Layers.
// IsShared: the module is in the Shared folder of the app, so it is the base of the same layer in each module.
// Uses: the modules in VsaModuleUses of the module root. Their Domain.Shared is visible to Domain.Shared and Presenters.
public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<string> Layers) {
    public bool IsShared { get; init; }

    public IReadOnlyList<string> Uses { get; init; } = [];

    // True when VSA.Sdk recorded the assembly as a module root.
    public static bool IsRoot(Assembly assembly) =>
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(a => a.Key == ModuleMetadata.Module);

    public static ModuleRoot Of(Assembly assembly) => new(
        assembly.GetName().Name!,
        assembly,
        [.. assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == ModuleMetadata.LayerAssembly && a.Value is not null)
            .Select(a => a.Value!)]) {
        IsShared = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(a => a.Key == ModuleMetadata.RootFolder && a.Value == ModuleMetadata.Shared),
        Uses = [.. assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == ModuleMetadata.ModuleUse && a.Value is not null)
            .Select(a => a.Value!)],
    };

    public static ModuleRoot Of(Module module) => Of(module.GetType().Assembly);

    public Assembly? LayerAssembly(string layer) {
        var name = $"{Name}.{layer}";
        return Layers.Contains(name) ? Assembly.Load(name) : null;
    }
}
