using System.Reflection;

namespace VSA.Module;

public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<Type> Classes) {
    public bool IsPlugin => Classes is [var root] && root.IsSubclassOf(typeof(Plugin));

    public static bool IsRoot(Assembly assembly) => RootClasses(assembly).Length > 0;

    public static IReadOnlyList<ModuleRoot> In(Assembly assembly) => [.. RootClasses(assembly)
        .GroupBy(NameOf, StringComparer.Ordinal)
        .Select(module => new ModuleRoot(module.Key, assembly, [.. module.OrderBy(type => type.FullName, StringComparer.Ordinal)]))];

    public static string NameOf(Type rootClass) => rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public Module CreateModule() => Classes switch {
        [var type] => (Module)Activator.CreateInstance(type)!,
        _ => throw new InvalidOperationException(
            $"Module {Name} has {Classes.Count} Module classes: "
            + $"{string.Join(", ", Classes.Select(t => t.FullName))}. A module must have one."),
    };

    static Type[] RootClasses(Assembly assembly) => [.. Types(assembly)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(typeof(Module)))];

    static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
