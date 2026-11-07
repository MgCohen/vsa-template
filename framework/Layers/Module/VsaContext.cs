using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VSA.Module;

public sealed class VsaContext(
    IServiceCollection services, IConfiguration configuration, IReadOnlyList<Module> modules) {
    Type[]? moduleTypes;

    public IServiceCollection Services => services;
    public IConfiguration Configuration => configuration;
    public IReadOnlyList<Module> Modules => modules;

    public IEnumerable<(Type Service, Type Implementation)> Implementations(Type baseType) {
        foreach (var type in ModuleTypes()) {
            if (ServiceOf(type, baseType) is { } service) {
                yield return (service, type);
            }
        }
    }

    Type[] ModuleTypes() => moduleTypes ??= [.. modules
        .SelectMany(module => ModuleRoot.Of(module).Layers)
        .SelectMany(layer => Assembly.Load(layer).GetTypes())
        .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

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
}
