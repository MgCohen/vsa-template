using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace VSA.Kernel;

public static class AnchorScan {
    public static IReadOnlyList<Anchor> Anchors(Type anchorBase) => InOrder(Roots(anchorBase).Select(root => root.Create()));

    public static IReadOnlyList<Anchor> InOrder(IEnumerable<Anchor> anchors) => [.. anchors
        .OrderBy(anchor => anchor.Order)
        .ThenBy(anchor => anchor.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<AnchorRoot> Roots(Type anchorBase) => [.. AssembliesThatReference(anchorBase.Assembly)
        .SelectMany(assembly => AnchorRoot.In(assembly, anchorBase))
        .OrderBy(root => root.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<IInstaller> Installers() => [.. AssembliesThatReference(typeof(IInstaller).Assembly)
        .Append(typeof(IInstaller).Assembly)
        .SelectMany(Types)
        .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IInstaller).IsAssignableFrom(t))
        .OrderBy(t => t.FullName, StringComparer.Ordinal)
        .Select(t => (IInstaller)Activator.CreateInstance(t)!)];

    public static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    public static IReadOnlyList<Assembly> AssembliesThatReference(Assembly referenced) {
        var name = referenced.GetName().Name!;
        var found = new List<Assembly>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (!assembly.IsDynamic && assembly.GetReferencedAssemblies().Any(r => r.Name == name)) {
                Add(found, assembly);
            }
        }
        foreach (var file in AssemblyFilesOfTheApp()) {
            if (ReadFile(file, name) is (var fileAssembly, true) && LoadOrNull(fileAssembly) is { } loaded) {
                Add(found, loaded);
            }
        }
        return found;
    }

    static void Add(List<Assembly> found, Assembly assembly) {
        if (!found.Contains(assembly)) {
            found.Add(assembly);
        }
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
