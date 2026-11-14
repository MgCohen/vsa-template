using System.Reflection;

namespace VSA.Kernel;

public sealed record AnchorRoot(string Name, Assembly Assembly, Type Base, IReadOnlyList<Type> Classes) {
    public static IReadOnlyList<AnchorRoot> In(Assembly assembly, Type anchorBase) => [.. RootClasses(assembly, anchorBase)
        .GroupBy(NameOf, StringComparer.Ordinal)
        .Select(anchor => new AnchorRoot(anchor.Key, assembly, anchorBase, [.. anchor.OrderBy(type => type.FullName, StringComparer.Ordinal)]))];

    public static string NameOf(Type rootClass) => rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public Anchor Create() => Classes switch {
        [var type] => (Anchor)Activator.CreateInstance(type)!,
        _ => throw new InvalidOperationException(
            $"{Base.Name} {Name} has {Classes.Count} {Base.Name} classes: "
            + $"{string.Join(", ", Classes.Select(t => t.FullName))}. A {Base.Name.ToLowerInvariant()} must have one."),
    };

    static Type[] RootClasses(Assembly assembly, Type anchorBase) => [.. AnchorScan.Types(assembly)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true } && t.IsSubclassOf(anchorBase))];
}
