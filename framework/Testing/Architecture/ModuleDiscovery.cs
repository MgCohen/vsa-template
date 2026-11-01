using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace VSA.Testing.Architecture;

// Finds the module roots of an app. VSA.Sdk records each module root as AssemblyMetadata("VsaModule", "<name>").
// The search reads all project libraries in the DependencyContext of the test run, the same as the host at start-up.
// It does not follow the references of the entry assembly: the compiler can drop an unused reference.
// TODO: use the public discovery API of issue #2 when it exists, so the host and the tests find the same modules.
static class ModuleDiscovery {
    public const string ModuleKey = "VsaModule";

    public static IReadOnlyList<Assembly> ModuleRoots(Assembly entry) {
        var context = DependencyContext.Default
            ?? throw new InvalidOperationException("The test run has no DependencyContext (.deps.json), so the modules cannot be found.");
        var projects = context.RuntimeLibraries.Where(l => l.Type == "project").ToList();
        var name = entry.GetName().Name!;
        if (!projects.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidOperationException($"{name} is not in the DependencyContext of the test run. Add a project reference to it.");
        }
        return [.. projects
            .SelectMany(l => l.GetDefaultAssemblyNames(context))
            .Select(Assembly.Load)
            .Where(a => a.GetCustomAttributes<AssemblyMetadataAttribute>().Any(m => m.Key == ModuleKey))];
    }
}
