namespace BuiltApp.Domain.Shared;

using System.Reflection;

public sealed record AppClass(
    TypeName Type, string Source, IReadOnlyList<TypeName> Packs, TypeName? Architecture, IReadOnlyList<string> Bases) {
    public const string EntryInterface = "Umpire.Kernels.IAppEntry`1";
    const string PackBase = "Umpire.Kernels.Pack`3";
    const string ArchitectureProperty = "Architecture";
    const char Nested = '+';
    const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public bool HasManyPacks => Packs.Count > 1;

    public static IReadOnlyList<Type> PacksOf(Type type) {
        if (type is not { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false }) {
            return [];
        }
        return [.. type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == EntryInterface)
            .Select(i => i.GetGenericArguments()[0])
            .OrderBy(pack => pack.FullName, StringComparer.Ordinal)];
    }

    public static Type? ArchitectureOf(Type pack) {
        for (var b = pack; b is not null; b = b.BaseType) {
            var definition = b.IsGenericType ? b.GetGenericTypeDefinition() : b;
            if (definition.FullName == PackBase) {
                return b.GetGenericArguments()[0];
            }
            if (b.GetProperty(ArchitectureProperty, Declared) is not null) {
                return null;
            }
        }
        return null;
    }

    public static IReadOnlyList<string> BasesOf(Type type) {
        var bases = new List<string>();
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            bases.Add((b.IsGenericType ? b.GetGenericTypeDefinition() : b).FullName!);
        }
        return bases;
    }

    public static IReadOnlyList<AppClass> Leaves(IReadOnlyList<AppClass> classes) =>
        [.. classes.Where(c => !classes.Any(other => other.Bases.Contains(c.Type.FullName) || other.Holds(c)))];

    bool Holds(AppClass inner) =>
        inner.Type.FullName.StartsWith(Type.FullName + Nested, StringComparison.Ordinal) && inner.Packs.SequenceEqual(Packs);
}
