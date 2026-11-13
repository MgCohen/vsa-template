using System.Reflection;
using System.Runtime.Loader;

namespace Architecture.Export;

internal static class AppFolder
{
    public static void Resolve(string folder)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) => Load(context, folder, name);
    }

    private static Assembly? Load(AssemblyLoadContext context, string folder, AssemblyName name)
    {
        string path = Path.Combine(Path.GetFullPath(folder), $"{name.Name}.dll");
        return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
    }
}
