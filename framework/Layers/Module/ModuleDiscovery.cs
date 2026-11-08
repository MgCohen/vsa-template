using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace VSA.Module;

public static class ModuleDiscovery {
    public static IReadOnlyList<Module> Modules(DependencyContext? context = null) =>
        [.. FindRoots(context).Select(root => root.CreateModule())];

    public static IReadOnlyList<ModuleRoot> FindRoots(DependencyContext? context = null) {
        context ??= DependencyContext.Default
            ?? throw new InvalidOperationException(
                "The app has no deps.json, so the modules cannot be found. Give the modules by hand.");
        return [.. context.RuntimeLibraries
            .Where(library => library.Type == "project")
            .Select(library => Assembly.Load(new AssemblyName(library.Name)))
            .Where(ModuleRoot.IsRoot)
            .Select(ModuleRoot.Of)
            .OrderBy(root => !root.IsShared)
            .ThenBy(root => root.Name, StringComparer.Ordinal)];
    }
}
