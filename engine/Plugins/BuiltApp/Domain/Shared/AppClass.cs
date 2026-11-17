namespace BuiltApp.Domain.Shared;

public sealed record AppClass(TypeName Type, string Source, bool HasEmptyConstructor, IReadOnlyList<string> Bases) {
    const string AppBase = "Umpire.Kernel.App";

    public static bool Is(Type type) =>
        type is { IsAbstract: false, ContainsGenericParameters: false } && BasesOf(type).Contains(AppBase);

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
