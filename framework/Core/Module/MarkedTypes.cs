using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VSA.Analyzers.Markers;

namespace VSA.Module;

public static class MarkedTypes {
    public static readonly string TypeArgumentName = typeof(TypeArgumentAttribute).FullName!;
    public static readonly string AutoRegisterName = typeof(AutoRegisterAttribute).FullName!;
    static readonly string AutoRegisterInterface = typeof(IAutoRegister).FullName!;

    public static bool IsTypeArgument(Type type) =>
        !type.ContainsGenericParameters
        && (type.IsValueType || type is { IsClass: true, IsAbstract: false })
        && Marker(type, TypeArgumentName) is not null;

    public static bool IsAutoRegistered(Type type) =>
        type is { IsClass: true, IsAbstract: false } && Marker(type, AutoRegisterName) is not null;

    public static ServiceLifetime LifetimeOf(Type type) =>
        Marker(type, AutoRegisterName) is { ConstructorArguments: [{ Value: int lifetime }] }
            ? (ServiceLifetime)lifetime
            : ServiceLifetime.Scoped;

    public static IEnumerable<Type> ServicesOf(Type type) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (Marker(b, AutoRegisterName) is not null) {
                yield return b;
            }
        }
        foreach (var i in type.GetInterfaces()) {
            if (i.FullName != AutoRegisterInterface && Marker(i, AutoRegisterName) is not null) {
                yield return i;
            }
        }
    }

    public static IEnumerable<Type> TypeArgumentsOf(Assembly assembly) =>
        assembly.GetCustomAttributesData()
            .Where(a => a.AttributeType.FullName == TypeArgumentName)
            .Select(a => a.ConstructorArguments is [{ Value: Type type }] ? type : null)
            .OfType<Type>();

    static CustomAttributeData? Marker(Type type, string name) {
        for (var t = type; t is not null; t = t.BaseType) {
            if (Declared(t, name) is { } marker) {
                return marker;
            }
        }
        return type.GetInterfaces().Select(i => Declared(i, name)).FirstOrDefault(m => m is not null);
    }

    static CustomAttributeData? Declared(Type type, string name) =>
        type.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.FullName == name);
}
