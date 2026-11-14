using System.Reflection;
using System.Runtime.Loader;
using VSA.Architecture;

namespace Architecture.Export;

internal static class AppFolder
{
    public static void Resolve(string folder)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) => Load(context, folder, name);
    }

    public static Assembly AppAssembly(string folder)
    {
        string deps = Directory.GetFiles(folder, "*.deps.json").Single();
        return Assembly.LoadFrom(Path.Combine(folder, Path.GetFileName(deps)[..^".deps.json".Length] + ".dll"));
    }

    public static ArchitectureDefinition Definition(Assembly app)
    {
        Type provider = app.GetTypes().Single(t => !t.IsAbstract
            && typeof(IArchitectureProvider).IsAssignableFrom(t)
            && t.GetConstructor(Type.EmptyTypes) is not null);
        return ((IArchitectureProvider)Activator.CreateInstance(provider)!).Architecture;
    }

    public static IEnumerable<Assembly> Externals(string folder, ArchitectureDefinition definition)
    {
        List<string> names = [.. definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct()];
        return Directory.GetFiles(folder, "*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => names.Any(e => name == e || name!.StartsWith(e + ".", StringComparison.Ordinal)))
            .Select(name => Assembly.Load(new AssemblyName(name!)));
    }

    private static Assembly? Load(AssemblyLoadContext context, string folder, AssemblyName name)
    {
        string path = Path.Combine(folder, $"{name.Name}.dll");
        return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
    }
}
