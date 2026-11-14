using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace VSA.Module;

public static class AppScan {
    public static IReadOnlyList<Module> Modules() => [.. ModuleRoots().Select(root => root.CreateModule())];

    public static IReadOnlyList<ModuleRoot> ModuleRoots() => [.. AssembliesThatReference(typeof(Module).Assembly)
        .SelectMany(ModuleRoot.In)
        .OrderBy(root => !root.IsPlugin)
        .ThenBy(root => root.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<Assembly> AssembliesThatReference(Assembly referenced) {
        var name = referenced.GetName().Name!;
        var found = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (!assembly.IsDynamic && assembly.GetReferencedAssemblies().Any(r => r.Name == name)) {
                found[assembly.GetName().Name!] = assembly;
            }
        }
        foreach (var file in AssemblyFilesOfTheApp()) {
            if (ReadFile(file, name) is (var fileAssembly, true) && !found.ContainsKey(fileAssembly)
                && LoadOrNull(fileAssembly) is { } loaded) {
                found[fileAssembly] = loaded;
            }
        }
        return [.. found.Values];
    }

    static IEnumerable<string> AssemblyFilesOfTheApp() =>
        Directory.Exists(AppContext.BaseDirectory)
            ? Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            : [];

    static (string Name, bool References)? ReadFile(string path, string referenced) {
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
            return (metadata.GetString(metadata.GetAssemblyDefinition().Name), references);
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
}
