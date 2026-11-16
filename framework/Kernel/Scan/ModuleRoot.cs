namespace Umpire.Kernel;

using System.Reflection;
using Umpire.Architecture;

public sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<Type> Classes) {
    public static IReadOnlyList<ModuleRoot> In(Assembly assembly) => In(assembly, LoadedTypes.Of(assembly));

    public Module Create() => Classes switch {
        [var type] when type.GetConstructor(Type.EmptyTypes) is null => throw new InvalidOperationException(
            $"Module {Name} cannot create {type.FullName}: it has no public constructor with no parameters. "
            + "Add one. Umpire creates the module class before the services exist."),
        [var type] => (Module)Activator.CreateInstance(type)!,
        _ => throw new InvalidOperationException(
            $"Module {Name} has {Classes.Count} Module classes: "
            + $"{string.Join(", ", Classes.Select(t => t.FullName))}. A module must have one."),
    };

    internal static IReadOnlyList<ModuleRoot> In(Assembly assembly, IEnumerable<Type> types) => [.. types
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(typeof(Module)))
        .GroupBy(AppTypes.NameOf, StringComparer.Ordinal)
        .Select(module => new ModuleRoot(
            module.Key, assembly, [.. module.OrderBy(type => type.FullName, StringComparer.Ordinal)]))];
}
