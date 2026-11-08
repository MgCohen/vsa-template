using System.Reflection;
using System.Runtime.CompilerServices;

namespace VSA.Module;

public sealed class ModuleTypes(IReadOnlyList<Module> modules) {
    Type[]? types;

    public IReadOnlyList<Module> Modules => modules;

    public IReadOnlyList<(Type Service, Type Implementation)> Implementations(Type baseType) =>
        [.. All()
            .Select(type => (Service: ServiceOf(type, baseType), Implementation: type))
            .Where(pair => pair.Service is not null)
            .Select(pair => (pair.Service!, pair.Implementation))];

    static Type? ServiceOf(Type type, Type baseType) {
        if (!baseType.IsGenericTypeDefinition) {
            return type.IsSubclassOf(baseType) ? baseType : null;
        }
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == baseType) {
                return b;
            }
        }
        return null;
    }

    Type[] All() => types ??= [.. modules
        .SelectMany(module => ModuleRoot.Of(module).Layers)
        .SelectMany(layer => Assembly.Load(layer).GetTypes())
        .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(CompilerGeneratedAttribute)))];
}
