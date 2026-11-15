namespace Umpire.Kernel;

using System.Reflection;

public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<Type> Classes) {
    public static IReadOnlyList<ModuleRoot> In(Assembly assembly) => [.. RootClasses(assembly)
        .GroupBy(AppTypes.NameOf, StringComparer.Ordinal)
        .Select(module => new ModuleRoot(
            module.Key, assembly, [.. module.OrderBy(type => type.FullName, StringComparer.Ordinal)]))];

    public Module Create() => Classes switch {
        [var type] => (Module)Activator.CreateInstance(type)!,
        _ => throw new InvalidOperationException(
            $"Module {Name} has {Classes.Count} Module classes: "
            + $"{string.Join(", ", Classes.Select(t => t.FullName))}. A module must have one."),
    };

    static Type[] RootClasses(Assembly assembly) => [.. AppTypes.Types(assembly)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(typeof(Module)))];
}
