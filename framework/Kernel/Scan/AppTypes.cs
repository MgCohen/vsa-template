namespace Umpire.Kernels;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Umpire.Architecture;

public sealed class AppTypes {
    readonly AppAssemblies scan = new();
    readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> concreteTypes = new();
    readonly Assembly? appAssembly;
    readonly IModuleProvider provider;
    readonly ModuleScan moduleScan;
    IReadOnlyList<Module>? modules;
    Assembly[]? assemblies;
    Assembly[]? scanned;
    Type[]? loaded;
    Type[]? registered;
    Type[]? autoCloseTypes;
    Type[]? openRegistered;
    Type[]? declared;

    public AppTypes(IModuleProvider? provider = null, Assembly? appAssembly = null) {
        this.provider = provider ?? new ScannedModules();
        moduleScan = new ModuleScan(scan);
        this.appAssembly = appAssembly;
    }

    public IReadOnlyList<Module> Modules => modules ??= InOrder(provider.Modules(moduleScan));

    internal IReadOnlyList<ModuleRoot> Roots => moduleScan.Roots;

    internal static IReadOnlyList<Module> InOrder(IEnumerable<Module> modules) => [.. modules
        .OrderBy(module => module.Order)
        .ThenBy(module => module.Name, StringComparer.Ordinal)];

    public static string NameOf(Type rootClass) =>
        rootClass.Namespace is { } name ? name[(name.LastIndexOf('.') + 1)..] : rootClass.Name;

    public IReadOnlyList<Type> ConcreteTypesOf(Type baseType) =>
        concreteTypes.GetOrAdd(baseType, FindConcreteTypes);

    public IReadOnlyList<Type> Registered() => registered ??= [.. Loaded()
        .Where(MarkedTypes.IsAutoRegistered)
        .SelectMany(type => type.IsGenericTypeDefinition ? OpenGenerics.CloseOver(type, AutoCloseTypes()) : [type])];

    public IReadOnlyList<Type> Declared() => declared ??= [.. Loaded()
        .Where(t => !t.ContainsGenericParameters && !t.IsAbstract && (t.IsClass || t.IsValueType))];

    public IReadOnlyList<Type> OpenRegistered() => openRegistered ??= [.. Loaded()
        .Where(type => type.IsGenericTypeDefinition && MarkedTypes.IsAutoRegistered(type))];

    public IReadOnlyList<ServiceImplementation> Implementations(Type baseType) =>
        [.. Registered()
            .Select(type => ServiceOf(type, baseType) is { } service ? new ServiceImplementation(service, type) : null)
            .OfType<ServiceImplementation>()];

    public IReadOnlyList<ServiceImplementation> OpenImplementations(Type baseType) =>
        [.. OpenRegistered()
            .Select(type => ServiceOf(type, baseType) is { } service ? new ServiceImplementation(service, type) : null)
            .OfType<ServiceImplementation>()];

    public IReadOnlyList<Type> AutoCloseTypes() => autoCloseTypes ??= [.. Loaded()
        .Where(MarkedTypes.IsAutoClose)
        .Concat(Scanned().SelectMany(MarkedTypes.AutoCloseTypesOf))
        .Distinct()];

    static Type? ServiceOf(Type type, Type baseType) {
        if (!baseType.IsGenericTypeDefinition) {
            return baseType.IsAssignableFrom(type) && type != baseType ? baseType : null;
        }
        return ClosedBase.Of(type, baseType) ?? ClosedBase.InterfaceOf(type, baseType);
    }

    IReadOnlyList<Type> FindConcreteTypes(Type baseType) => [.. Scanned()
        .SelectMany(scan.TypesOf)
        .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
        .Where(t => t.IsSubclassOf(baseType))];

    Type[] Loaded() => loaded ??= [.. Scanned()
        .SelectMany(scan.TypesOf)
        .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)))];

    Assembly[] Assemblies() => assemblies ??= [.. Modules.Select(module => module.GetType().Assembly).Distinct()];

    Assembly[] Scanned() => scanned ??= [.. Assemblies().Append(appAssembly).OfType<Assembly>().Distinct()];
}
