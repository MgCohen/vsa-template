using System.Reflection;
using System.Runtime.CompilerServices;

namespace VSA.Module;

public sealed class ModuleTypes(IReadOnlyList<Module> modules, IKeySource keys) {
    Assembly[]? assemblies;
    Type[]? loaded;
    Type[]? registered;
    Type[]? typeArguments;

    public ModuleTypes(IReadOnlyList<Module> modules) : this(modules, new ConstraintKeys()) {
    }

    public IReadOnlyList<Module> Modules => modules;

    public IReadOnlyList<Type> Registered() => registered ??= [.. Loaded()
        .Where(MarkedTypes.IsAutoRegistered)
        .SelectMany(type => type.IsGenericTypeDefinition ? Close(type) : [type])];

    public IReadOnlyList<(Type Service, Type Implementation)> Implementations(Type baseType) =>
        [.. Registered()
            .Select(type => (Service: ServiceOf(type, baseType), Implementation: type))
            .Where(pair => pair.Service is not null)
            .Select(pair => (pair.Service!, pair.Implementation))];

    public IReadOnlyList<Type> TypeArguments() => typeArguments ??= [.. Loaded()
        .Where(MarkedTypes.IsTypeArgument)
        .Concat(Assemblies().SelectMany(MarkedTypes.TypeArgumentsOf))
        .Distinct()];

    static Type? ServiceOf(Type type, Type baseType) {
        if (!baseType.IsGenericTypeDefinition) {
            return baseType.IsAssignableFrom(type) && type != baseType ? baseType : null;
        }
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == baseType) {
                return b;
            }
        }
        return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType);
    }

    IEnumerable<Type> Close(Type definition) {
        var parameters = definition.GetGenericArguments();
        if (parameters.Length != 1) {
            throw new InvalidOperationException(
                $"{definition.FullName} has {parameters.Length} generic parameters. "
                + "VSA closes an open module type with one generic parameter only. "
                + "Write one closed type for each type argument.");
        }
        return keys.KeysFor(parameters[0], TypeArguments()).Select(key => definition.MakeGenericType(key));
    }

    Type[] Loaded() => loaded ??= [.. Assemblies()
        .SelectMany(assembly => assembly.GetTypes())
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

    Assembly[] Assemblies() => assemblies ??= [.. modules
        .Select(ModuleRoot.Of)
        .SelectMany(root => root.Layers.Select(Assembly.Load).Prepend(root.Assembly))
        .Distinct()];
}
