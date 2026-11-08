using System.Reflection;

namespace VSA.Module;

// A module root assembly and the records that VSA.Sdk wrote in it at build time.
// Name is the assembly name, the same as Module.Name. Layers are the assembly names of the layer projects
// of the module, for example "Notes.Handlers.Requests". A layer that the module does not have is not in Layers.
// IsPlugin: the module is in the Plugins folder of the app, so it is the base of the same layer in each module.
// Uses: the modules in VsaModuleUses of the module root. Their Domain.Shared is visible to Domain.Shared and Presenters.
public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<string> Layers) {
    public bool IsPlugin { get; init; }

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
        IsPlugin = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(a => a.Key == ModuleMetadata.RootFolder && a.Value == ModuleMetadata.Plugins),
        Uses = [.. assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == ModuleMetadata.ModuleUse && a.Value is not null)
            .Select(a => a.Value!)],
    };

    public static ModuleRoot Of(Module module) => Of(module.GetType().Assembly);

    public Module CreateModule() {
        var classes = Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Module)))
            .ToArray();
        return classes switch {
            [var type] when IsPlugin && !type.IsSubclassOf(typeof(Plugin)) => throw new InvalidOperationException(
                $"{type.FullName} is the root class of the plugin module {Name}, but it inherits Module. "
                + "Make it inherit Plugin or Plugin<TSettings>."),
            [var type] when !IsPlugin && type.IsSubclassOf(typeof(Plugin)) => throw new InvalidOperationException(
                $"{type.FullName} is the root class of the module {Name}, but it inherits Plugin. "
                + "Make it inherit Module or Module<TSettings>, or move the module to Plugins/."),
            [var type] => (Module)Activator.CreateInstance(type)!,
            [] => throw new InvalidOperationException(
                $"Module {Name} has no Module class. "
                + $"Add one class that inherits Module or Module<TSettings> to the {Name} module root."),
            _ => throw new InvalidOperationException(
                $"Module {Name} has {classes.Length} Module classes: "
                + $"{string.Join(", ", classes.Select(t => t.FullName))}. A module root must have one."),
        };
    }
}
