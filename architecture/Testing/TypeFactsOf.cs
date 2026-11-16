namespace Umpire.Architecture;

using System.Reflection;

static class TypeFactsOf {
    public static TypeFacts Type(Type type, Source source) => new(
        type.FullName ?? type.Name,
        type.Name,
        type.Assembly.GetName().Name ?? "",
        source,
        [.. Bases(type).Select(NameOf).Distinct()],
        [],
        [],
        []) {
        BaseArguments = [.. Arguments(type)],
        IsAbstract = type.IsAbstract,
    };

    public static TypeFacts Plain(string name, Source source) => new(name, name, "", source, [name], [], [], []);

    static IEnumerable<Type> Bases(Type type) => new[] { type }.Concat(Inherited(type)).Concat(type.GetInterfaces());

    static IEnumerable<Type> Inherited(Type type) {
        var current = type.GetTypeInfo().BaseType;
        while (current is not null && current != typeof(object)) {
            yield return current;
            current = current.BaseType;
        }
    }

    static IEnumerable<string> Arguments(Type type) =>
        Bases(type).Where(b => b.IsGenericType).SelectMany(b => b.GetGenericArguments()).Select(NameOf).Distinct();

    static string NameOf(Type type) {
        var open = type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
        return open.FullName ?? open.Name;
    }
}
