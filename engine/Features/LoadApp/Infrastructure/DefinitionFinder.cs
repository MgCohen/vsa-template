namespace LoadApp.Infrastructure;

using System.Reflection;
using Umpire.Architecture;
using Umpire.Kernel;

public static class DefinitionFinder {
    const string AppClass = $"{nameof(Umpire)}.{nameof(Umpire.Kernel)}.{nameof(App)}";

    public static AppClasses Find(IReadOnlyList<Assembly> app) {
        var classes = Leaves([.. app.SelectMany(LoadedTypes.Of).Where(IsAppClass)]);
        return new AppClasses(
            [.. classes.Where(HasEmptyConstructor)],
            [.. classes.Where(type => !HasEmptyConstructor(type))]);
    }

    public static Architecture ArchitectureOf(Type appClass) {
        var app = Activator.CreateInstance(appClass)!;
        var pack = appClass.GetProperty(nameof(App.Pack))!.GetValue(app)!;
        return (Architecture)pack.GetType().GetProperty(nameof(Pack.Architecture))!.GetValue(pack)!;
    }

    public static string Names(IEnumerable<Type> types) =>
        string.Join(", ", types.Select(t => t.FullName).Order(StringComparer.Ordinal));

    static bool IsAppClass(Type type) =>
        type is { IsAbstract: false, ContainsGenericParameters: false } && InheritsApp(type);

    static bool HasEmptyConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) is not null;

    static bool InheritsApp(Type type) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.FullName == AppClass) {
                return true;
            }
        }
        return false;
    }

    static List<Type> Leaves(List<Type> types) =>
        [.. types.Where(t => !types.Any(other => other != t && t.IsAssignableFrom(other)))];
}
