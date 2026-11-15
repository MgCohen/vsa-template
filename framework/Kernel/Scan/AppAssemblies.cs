namespace Umpire.Kernel;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class AppAssemblies {
    const string TestFramework = "nunit.framework";

    internal static IReadOnlyList<Assembly> OfModules() {
        var found = new List<Assembly>();
        var seen = new HashSet<Assembly>();
        var bases = new Queue<Assembly>([typeof(Module).Assembly]);
        while (bases.TryDequeue(out var moduleBase)) {
            if (!seen.Add(moduleBase)) {
                continue;
            }
            foreach (var assembly in ThatReference(moduleBase).Where(assembly => !IsTestAssembly(assembly))) {
                Add(found, assembly);
                if (DefinesModuleBase(assembly)) {
                    bases.Enqueue(assembly);
                }
            }
        }
        return found;
    }

    internal static IReadOnlyList<Assembly> ThatReference(Assembly referenced) {
        var name = referenced.GetName().Name!;
        var found = new List<Assembly>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (!assembly.IsDynamic && assembly.GetReferencedAssemblies().Any(r => r.Name == name)) {
                Add(found, assembly);
            }
        }
        foreach (var file in Files()) {
            if (Read(file, name) is { References: true } read && LoadOrNull(read.Name) is { } loaded) {
                Add(found, loaded);
            }
        }
        return found;
    }

    static bool IsTestAssembly(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Any(reference => reference.Name == TestFramework);

    static bool DefinesModuleBase(Assembly assembly) =>
        AppTypes.Types(assembly).Any(t => t is { IsClass: true, IsAbstract: true } && t.IsSubclassOf(typeof(Module)));

    static void Add(List<Assembly> found, Assembly assembly) {
        if (!found.Contains(assembly)) {
            found.Add(assembly);
        }
    }

    static IEnumerable<string> Files() =>
        Directory.Exists(AppContext.BaseDirectory)
            ? Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            : [];

    static AssemblyFile? Read(string path, string referenced) {
        try {
            using var stream = File.OpenRead(path);
            using var file = new PEReader(stream);
            if (!file.HasMetadata) {
                return null;
            }
            var metadata = file.GetMetadataReader();
            if (!metadata.IsAssembly) {
                return null;
            }
            var references = metadata.AssemblyReferences
                .Any(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name) == referenced);
            return new AssemblyFile(metadata.GetString(metadata.GetAssemblyDefinition().Name), references);
        } catch (Exception e) when (e is BadImageFormatException or IOException or UnauthorizedAccessException) {
            return null;
        }
    }

    static Assembly? LoadOrNull(string name) {
        try {
            return Assembly.Load(new AssemblyName(name));
        } catch (FileNotFoundException) {
            return null;
        }
    }

    sealed record AssemblyFile(string Name, bool References);
}
