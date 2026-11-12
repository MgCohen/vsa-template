using System.Reflection;

namespace VSA.Module;

// A module root assembly: an assembly with a public, top-level class that inherits Module or Plugin.
// Name is the assembly name, the same as Module.Name. Layers are the assembly names of the layer projects
// of the module, for example "Notes.Handlers.Requests": each assembly named <Name>.<layer> that loads, for each
// name in LayerNames. A layer that the module does not have is not in Layers.
// IsPlugin: the root class inherits Plugin, so the module is the base of the same layer in each module.
// VSA1014 makes the root class agree with the root folder at build time.
public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<string> Layers) {
    // The layers of a module project, from the lowest. VSA0004 checks the same names on the project files.
    public static readonly IReadOnlyList<string> LayerNames = [
        "Domain.Shared", "Domain", "Infrastructure",
        "Contracts.Requests", "Contracts.Events", "Contracts.Commands",
        "Handlers.Requests", "Handlers.Events", "Handlers.Commands",
        "Presenters",
    ];

    public bool IsPlugin { get; init; }

    public static bool IsRoot(Assembly assembly) => RootClasses(assembly).Length > 0;

    public static ModuleRoot Of(Assembly assembly) {
        var name = assembly.GetName().Name!;
        return new(name, assembly, [.. LayerNames.Select(layer => $"{name}.{layer}").Where(Loads)]) {
            IsPlugin = RootClasses(assembly) is [var root] && root.IsSubclassOf(typeof(Plugin)),
        };
    }

    public static ModuleRoot Of(Module module) => Of(module.GetType().Assembly);

    public Module CreateModule() {
        var classes = RootClasses(Assembly);
        return classes switch {
            [var type] => (Module)Activator.CreateInstance(type)!,
            [] => throw new InvalidOperationException(
                $"Module {Name} has no Module class. "
                + $"Add one class that inherits Module or Module<TSettings> to the {Name} module root."),
            _ => throw new InvalidOperationException(
                $"Module {Name} has {classes.Length} Module classes: "
                + $"{string.Join(", ", classes.Select(t => t.FullName))}. A module root must have one."),
        };
    }

    // A nested or internal class that inherits Module, for example in a test project, does not make a module root.
    static Type[] RootClasses(Assembly assembly) => [.. Types(assembly)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(typeof(Module)))];

    static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    static bool Loads(string assemblyName) {
        try {
            Assembly.Load(new AssemblyName(assemblyName));
            return true;
        } catch (FileNotFoundException) {
            return false;
        }
    }
}
