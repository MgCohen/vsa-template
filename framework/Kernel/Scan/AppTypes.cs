namespace Umpire.Kernels;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Umpire.Architecture;

public sealed class AppTypes {
    readonly AppAssemblies scan = new();
    readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> concreteTypes = new();
    readonly Assembly? appAssembly;
    IReadOnlyList<Module>? modules;
    IReadOnlyList<ModuleRoot>? roots;
    Assembly[]? assemblies;
    Type[]? loaded;
    Type[]? registered;
    Type[]? typeArguments;

    public AppTypes(IReadOnlyList<Module>? modules, Assembly? appAssembly = null) {
        this.modules = modules is null ? null : InOrder(modules);
        this.appAssembly = appAssembly;
    }

    public IReadOnlyList<Module> Modules => modules ??= InOrder(Roots.Select(root => root.Create()));

    internal IReadOnlyList<ModuleRoot> Roots => roots ??= [.. scan.OfModules()
        .SelectMany(assembly => ModuleRoot.In(assembly, scan.TypesOf(assembly)))
        .OrderBy(root => root.Name, StringComparer.Ordinal)];

    internal static IReadOnlyList<Module> InOrder(IEnumerable<Module> modules) => [.. modules
        .OrderBy(module => module.Order)
        .ThenBy(module => module.Name, StringComparer.Ordinal)];

    public static string NameOf(Type rootClass) =>
        rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public IReadOnlyList<Type> ConcreteTypesOf(Type baseType) =>
        concreteTypes.GetOrAdd(baseType, FindConcreteTypes);

    public IReadOnlyList<Type> Registered() => registered ??= [.. Loaded()
        .Where(MarkedTypes.IsAutoRegistered)
        .SelectMany(type => type.IsGenericTypeDefinition ? Close(type) : [type])];

    public IReadOnlyList<ServiceImplementation> Implementations(Type baseType) =>
        [.. Registered()
            .Select(type => ServiceOf(type, baseType) is { } service ? new ServiceImplementation(service, type) : null)
            .OfType<ServiceImplementation>()];

    internal IReadOnlyList<Type> TypeArguments() => typeArguments ??= [.. Loaded()
        .Where(MarkedTypes.IsTypeArgument)
        .Concat(Assemblies().SelectMany(MarkedTypes.TypeArgumentsOf))
        .Distinct()];

    static Type? ServiceOf(Type type, Type baseType) {
        if (!baseType.IsGenericTypeDefinition) {
            return baseType.IsAssignableFrom(type) && type != baseType ? baseType : null;
        }
        return ClosedBase.Of(type, baseType) ?? ClosedBase.InterfaceOf(type, baseType);
    }

    IEnumerable<Type> Close(Type definition) {
        var parameters = definition.GetGenericArguments();
        if (parameters.Length != 1) {
            throw new InvalidOperationException(
                $"{definition.FullName} has {parameters.Length} generic parameters. "
                + "Umpire closes an open module type with one generic parameter only. "
                + "Write one closed type for each type argument.");
        }
        return ConstraintTypeArguments.For(parameters[0], TypeArguments())
            .Select(argument => definition.MakeGenericType(argument));
    }

    IReadOnlyList<Type> FindConcreteTypes(Type baseType) => [.. Assemblies()
        .Append(appAssembly)
        .OfType<Assembly>()
        .Distinct()
        .SelectMany(scan.TypesOf)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
        .Where(t => t.IsSubclassOf(baseType))];

    Type[] Loaded() => loaded ??= [.. Assemblies()
        .SelectMany(scan.TypesOf)
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

    Assembly[] Assemblies() => assemblies ??= [.. Modules.Select(module => module.GetType().Assembly).Distinct()];
}
