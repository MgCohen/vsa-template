using System.Reflection;
using System.Runtime.CompilerServices;

namespace VSA.Module;

public sealed class ModuleTypes(IReadOnlyList<Module> modules, IKeySource keys) {
    Type[]? loaded;
    Type[]? concrete;
    Type[]? keyCandidates;

    public ModuleTypes(IReadOnlyList<Module> modules) : this(modules, new ConstraintKeys()) {
    }

    public IReadOnlyList<Module> Modules => modules;

    public IReadOnlyList<(Type Service, Type Implementation)> Implementations(Type baseType) =>
        [.. Concrete()
            .Where(type => ServiceOf(type, baseType) is not null)
            .SelectMany(type => type.IsGenericTypeDefinition ? Close(type) : [type])
            .Select(type => (Service: ServiceOf(type, baseType)!, Implementation: type))];

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

    IEnumerable<Type> Close(Type definition) {
        var parameters = definition.GetGenericArguments();
        if (parameters.Length != 1) {
            throw new InvalidOperationException(
                $"{definition.FullName} has {parameters.Length} generic parameters. "
                + "VSA closes an open module type with one generic parameter only. "
                + "Write one closed type for each key.");
        }
        return keys.KeysFor(parameters[0], KeyCandidates()).Select(key => definition.MakeGenericType(key));
    }

    Type[] Concrete() => concrete ??= [.. Loaded().Where(t => t is { IsClass: true, IsAbstract: false })];

    Type[] KeyCandidates() => keyCandidates ??= [.. Loaded().Where(KeyTypes.IsKey)];

    Type[] Loaded() => loaded ??= [.. modules
        .SelectMany(module => ModuleRoot.Of(module).Layers)
        .SelectMany(layer => Assembly.Load(layer).GetTypes())
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];
}
