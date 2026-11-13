using System.Reflection;
using VSA.Module;

namespace VSA.Composition;

public static class VsaHosts {
    public static IVsaHost Find() => Find(AppScan.AssembliesThatReference(typeof(IVsaHost).Assembly));

    public static IVsaHost Find(IEnumerable<Assembly> assemblies) {
        var found = assemblies.Distinct().SelectMany(TypesOf).Where(IsHost).ToList();
        return found switch {
            [] => new GenericVsaHost(),
            [var host] => (IVsaHost)Activator.CreateInstance(host, nonPublic: true)!,
            _ => throw new InvalidOperationException(
                $"The app has {found.Count} hosts: {string.Join(", ", found.Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal))}. "
                + "Remove the reference to one host, or select one in the Builder step of the app: VsaBuilder.Create(args).UseHost(...)."),
        };
    }

    static bool IsHost(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
        && typeof(IVsaHost).IsAssignableFrom(type)
        && type != typeof(GenericVsaHost)
        && type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) is not null;

    static IEnumerable<Type> TypesOf(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
