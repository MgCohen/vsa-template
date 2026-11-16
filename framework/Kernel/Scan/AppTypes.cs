namespace Umpire.Kernel;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Umpire.Architecture;

public sealed class AppTypes {
    readonly IKeySource keys;
    readonly AppAssemblies scan = new();
    readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> concreteTypes = new();
    IReadOnlyList<Module>? modules;
    IReadOnlyList<ModuleRoot>? roots;
    Assembly[]? assemblies;
    Type[]? loaded;
    Type[]? registered;
    Type[]? typeArguments;

    public AppTypes(IReadOnlyList<Module> modules, IKeySource keys) {
        this.keys = keys;
        this.modules = InOrder(modules);
    }

    public AppTypes(IReadOnlyList<Module> modules) : this(modules, new ConstraintKeys()) {
    }

    AppTypes(IKeySource keys) {
        this.keys = keys;
    }

    public IReadOnlyList<Module> Modules => modules ??= InOrder(Roots.Select(root => root.Create()));

    public IReadOnlyList<ModuleRoot> Roots => roots ??= [.. scan.OfModules()
        .SelectMany(assembly => ModuleRoot.In(assembly, scan.TypesOf(assembly)))
        .OrderBy(root => root.Name, StringComparer.Ordinal)];

    public static AppTypes Find(IKeySource keys) => new(keys);

    public static IReadOnlyList<Module> InOrder(IEnumerable<Module> modules) => [.. modules
        .OrderBy(module => module.Order)
        .ThenBy(module => module.Name, StringComparer.Ordinal)];

    public static string NameOf(Type rootClass) =>
        rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public static IEnumerable<Type> Types(Assembly assembly) => LoadedTypes.Of(assembly);

    public IReadOnlyList<Type> ConcreteTypesOf(Type baseType) =>
        concreteTypes.GetOrAdd(baseType, FindConcreteTypes);

    public IReadOnlyList<Type> Registered() => registered ??= [.. Loaded()
        .Where(MarkedTypes.IsAutoRegistered)
        .SelectMany(type => type.IsGenericTypeDefinition ? Close(type) : [type])];

    public IReadOnlyList<Registration> Implementations(Type baseType) =>
        [.. Registered()
            .Select(type => ServiceOf(type, baseType) is { } service ? new Registration(service, type) : null)
            .OfType<Registration>()];

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
                + "Umpire closes an open module type with one generic parameter only. "
                + "Write one closed type for each type argument.");
        }
        return keys.KeysFor(parameters[0], TypeArguments()).Select(key => definition.MakeGenericType(key));
    }

    IReadOnlyList<Type> FindConcreteTypes(Type baseType) => [.. scan.ThatReference(baseType.Assembly)
        .SelectMany(scan.TypesOf)
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(baseType))];

    Type[] Loaded() => loaded ??= [.. Assemblies()
        .SelectMany(scan.TypesOf)
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

    Assembly[] Assemblies() => assemblies ??= [.. Modules.Select(module => module.GetType().Assembly).Distinct()];
}
