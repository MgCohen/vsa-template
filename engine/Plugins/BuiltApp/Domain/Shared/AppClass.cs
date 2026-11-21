namespace BuiltApp.Domain.Shared;

using System.Reflection;

public sealed record AppClass(TypeName Type, string Source, TypeName Pack, TypeName? Architecture, IReadOnlyList<string> Bases) {
    public const string EntryInterface = "Umpire.Kernels.IAppEntry`1";
    const string PackBase = "Umpire.Kernels.Pack`3";
    const string ArchitectureProperty = "Architecture";
    const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static Type? PackOf(Type type) {
        if (type is not { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false }) {
            return null;
        }
        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == EntryInterface)?
            .GetGenericArguments()[0];
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
        [.. classes.Where(c => !classes.Any(other => other.Bases.Contains(c.Type.FullName)))];
}
