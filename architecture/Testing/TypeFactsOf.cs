namespace Umpire.Architecture;

using System.Reflection;

static class TypeFactsOf {
    const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static TypeFacts Type(Type type, Source source) => new(
        type.FullName ?? type.Name,
        type.Name,
        type.Assembly.GetName().Name ?? "",
        source,
        [.. Bases(type).Select(NameOf).Distinct()],
        [],
        [.. Mentions(type).Where(t => t != type).Select(NameOf).Distinct()],
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

    static IEnumerable<string> Arguments(Type type) => new[] { type }.Concat(Inherited(type))
        .SelectMany(DirectBases)
        .Where(b => b.IsGenericType)
        .SelectMany(b => b.GetGenericArguments())
        .Select(NameOf)
        .Distinct();

    static IEnumerable<Type> DirectBases(Type type) {
        var inherited = type.BaseType?.GetInterfaces() ?? [];
        var own = type.GetInterfaces().Where(i => !inherited.Contains(i));
        return type.BaseType is { } baseType ? new[] { baseType }.Concat(own) : own;
    }

    static IEnumerable<Type> Mentions(Type type) => DirectBases(type)
        .Concat(type.GetFields(Declared).Select(f => f.FieldType))
        .Concat(type.GetProperties(Declared).Select(p => p.PropertyType))
        .Concat(type.GetConstructors(Declared).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType))
        .Concat(type.GetMethods(Declared).SelectMany(Signature))
        .SelectMany(Expand);

    static IEnumerable<Type> Signature(MethodInfo method) =>
        method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType);

    static IEnumerable<Type> Expand(Type type) {
        if (type.HasElementType && type.GetElementType() is { } element) {
            return Expand(element);
        }
        return type.IsGenericType ? new[] { type }.Concat(type.GetGenericArguments().SelectMany(Expand)) : [type];
    }

    static string NameOf(Type type) {
        var open = type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
        return open.FullName ?? open.Name;
    }
}
