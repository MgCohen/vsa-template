namespace Umpire.Architecture;

using System.Reflection;
using System.Runtime.CompilerServices;
using Umpire.Utility;

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
        IsAbstract = type.IsAbstract && !type.IsSealed,
        IsStatic = type is { IsClass: true, IsAbstract: true, IsSealed: true },
        IsSealed = type is { IsClass: true, IsSealed: true, IsAbstract: false } && !IsDelegate(type),
        IsRecord = IsRecord(type),
        IsStruct = type is { IsValueType: true, IsEnum: false },
        IsInterface = type.IsInterface,
        IsEnum = type.IsEnum,
        IsDelegate = IsDelegate(type),
        Attributes = [.. Attributes(type)],
        MutableMembers = [.. MutableMembers(type)],
        References = [.. Referenced(type).Where(t => t != type).Select(NameOf).Distinct()],
    };

    public static TypeFacts Plain(string name, Source source) => new(name, name, "", source, [name], [], [], []);

    static IEnumerable<string> MutableMembers(Type type) => type.IsEnum ? [] : type.GetFields(Declared)
        .Where(f => !f.IsInitOnly && !f.IsLiteral && !LoadedTypes.IsCompilerGenerated(f.Name))
        .Select(f => f.Name)
        .Concat(type.GetProperties(Declared).Where(IsSettable).Select(p => p.Name));

    static bool IsRecord(Type type) => type.IsClass
        ? type.GetMethod("<Clone>$", Declared) is not null
        : type.IsValueType && type.GetMethod("PrintMembers", Declared, null, [typeof(System.Text.StringBuilder)], null) is not null;

    static bool IsSettable(PropertyInfo property) =>
        property.SetMethod is { } set && set.ReturnParameter.GetRequiredCustomModifiers().All(m => m.Name != "IsExternalInit");

    static IEnumerable<string> Attributes(Type type) => type.GetCustomAttributesData()
        .Concat(type.GetMembers(Declared).SelectMany(m => m.GetCustomAttributesData()))
        .SelectMany(a => new[] { a.AttributeType }.Concat(Inherited(a.AttributeType)))
        .Select(NameOf)
        .Distinct();

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

    static IEnumerable<Type> Referenced(Type type) => DirectBases(type)
        .Concat(type.GetFields(Declared).Where(IsWritten).Select(f => f.FieldType))
        .Concat(type.GetProperties(Declared).Where(IsWritten).Select(p => p.PropertyType))
        .Concat(type.GetConstructors(Declared).Where(IsWritten).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType))
        .Concat(type.GetMethods(Declared).Where(IsWritten).SelectMany(Signature))
        .SelectMany(Expand);

    static bool IsWritten(MemberInfo member) => !member.IsDefined(typeof(CompilerGeneratedAttribute), false);

    static bool IsDelegate(Type type) => type.IsClass && typeof(MulticastDelegate).IsAssignableFrom(type);

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
