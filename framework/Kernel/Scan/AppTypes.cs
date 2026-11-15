namespace Umpire.Kernel;

using System.Reflection;
using System.Runtime.CompilerServices;

public sealed class AppTypes(IReadOnlyList<Module> modules, IKeySource keys) {
    Assembly[]? assemblies;
    Type[]? loaded;
    Type[]? registered;
    Type[]? typeArguments;

    public AppTypes(IReadOnlyList<Module> modules) : this(modules, new ConstraintKeys()) {
    }

    public IReadOnlyList<Module> Modules { get; } = InOrder(modules);

    public static IReadOnlyList<Module> FindModules() => InOrder(Roots().Select(root => root.Create()));

    public static IReadOnlyList<ModuleRoot> Roots() => [.. AppAssemblies.OfModules()
        .SelectMany(ModuleRoot.In)
        .OrderBy(root => root.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<Module> InOrder(IEnumerable<Module> modules) => [.. modules
        .OrderBy(module => module.Order)
        .ThenBy(module => module.Name, StringComparer.Ordinal)];

    public static string NameOf(Type rootClass) =>
        rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    public static IReadOnlyList<Assembly> AssembliesThatReference(Assembly referenced) =>
        AppAssemblies.ThatReference(referenced);

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

    Type[] Loaded() => loaded ??= [.. Assemblies()
        .SelectMany(Types)
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

    Assembly[] Assemblies() => assemblies ??= [.. Modules.Select(module => module.GetType().Assembly).Distinct()];
}
