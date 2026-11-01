using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace VSA.Module;

// v1: run-time discovery from deps.json. A source generator in the host can replace it in v2.
// The host (AddVsa) and the architecture tests use this type, so the two find the same modules.
public static class ModuleDiscovery {
    // Loads each project library of the context and keeps the module roots (ModuleRoot.IsRoot).
    // The order is by name, so startup is the same each time. The default context is DependencyContext.Default.
    public static IReadOnlyList<ModuleRoot> FindRoots(DependencyContext? context = null) {
        context ??= DependencyContext.Default
            ?? throw new InvalidOperationException("The app has no deps.json, so the modules cannot be found. Give the modules by hand.");
        return [.. context.RuntimeLibraries
            .Where(library => library.Type == "project")
            .Select(library => Assembly.Load(new AssemblyName(library.Name)))
            .Where(ModuleRoot.IsRoot)
            .Select(ModuleRoot.Of)
            .OrderBy(root => root.Name, StringComparer.Ordinal)];
    }

    // Creates the one concrete Module class of each root, in the order of the roots.
    // A root with no Module class, or with two, is an error.
    public static IReadOnlyList<Module> CreateModules(IEnumerable<ModuleRoot> roots) => [.. roots.Select(Create)];

    static Module Create(ModuleRoot root) {
        var classes = root.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Module)))
            .ToArray();
        return classes switch {
            [var type] => (Module)Activator.CreateInstance(type)!,
            [] => throw new InvalidOperationException($"Module {root.Name} has no Module class. Add one class that inherits Module or Module<TSettings> to the {root.Name} module root."),
            _ => throw new InvalidOperationException($"Module {root.Name} has {classes.Length} Module classes: {string.Join(", ", classes.Select(t => t.FullName))}. A module root must have one."),
        };
    }
}
