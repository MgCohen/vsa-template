namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;

static class ReflectionShapes {
    public static DllShape Of(Type type) => new(Text(type), [.. Named(type).Select(NameOf).Distinct()]);

    public static TypeName NameOf(Type type) {
        var definition = Definition(type);
        return new TypeName(FullName(definition), AssemblyOf(definition));
    }

    public static IEnumerable<Type> Named(Type type) {
        if (Element(type) is { } element) {
            return Named(element);
        }
        if (type.IsGenericParameter) {
            return [];
        }
        return new[] { type }.Concat(Arguments(type).SelectMany(Named));
    }

    public static IReadOnlyList<Type> Arguments(Type type) =>
        type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericArguments() : [];

    public static Type Definition(Type type) =>
        type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;

    public static string FullName(Type type) {
        if (type.IsNested && type.DeclaringType is { } outer) {
            return $"{FullName(Definition(outer))}+{type.Name}";
        }
        return string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    static string Text(Type type) {
        if (type.IsByRef) {
            return Text(type.GetElementType()!);
        }
        if (type.IsArray) {
            return $"{Text(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        }
        if (type.IsPointer) {
            return Text(type.GetElementType()!) + "*";
        }
        if (type.IsGenericParameter) {
            return type.Name;
        }
        var name = FullName(Definition(type));
        var arguments = Arguments(type);
        return arguments.Count == 0 ? name : $"{name}<{string.Join(",", arguments.Select(Text))}>";
    }

    static Type? Element(Type type) => type.IsByRef || type.IsArray || type.IsPointer ? type.GetElementType() : null;

    static string AssemblyOf(Type type) {
        try {
            return type.Assembly.GetName().Name ?? "";
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return "";
        }
    }
}
