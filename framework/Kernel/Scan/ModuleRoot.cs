namespace Umpire.Kernels;

using System.Reflection;
using Umpire.Utility;

sealed record ModuleRoot(string Name, Assembly Assembly, IReadOnlyList<Type> Classes) {
    internal static IReadOnlyList<ModuleRoot> In(Assembly assembly) => In(assembly, LoadedTypes.Of(assembly));

    internal Module Create() {
        if (Classes.Count != 1) {
            throw new InvalidOperationException(
                $"Module {Name} has {Classes.Count} Module classes: "
                + $"{string.Join(", ", Classes.Select(t => t.FullName))}. A module must have one.");
        }
        var type = Classes[0];
        if (type.GetConstructor(Type.EmptyTypes) is null) {
            throw new InvalidOperationException(
                $"Module {Name} cannot create {type.FullName}: it has no public constructor with no parameters. "
                + "Add one. Umpire creates the module class before the services exist.");
        }
        return (Module)Activator.CreateInstance(type)!;
    }

    internal static IReadOnlyList<ModuleRoot> In(Assembly assembly, IEnumerable<Type> types) => [.. types
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(typeof(Module)))
        .GroupBy(AppTypes.NameOf, StringComparer.Ordinal)
        .Select(module => new ModuleRoot(
            module.Key, assembly, [.. module.OrderBy(type => type.FullName, StringComparer.Ordinal)]))];
}
