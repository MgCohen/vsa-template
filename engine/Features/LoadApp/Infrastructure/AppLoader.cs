namespace LoadApp.Infrastructure;

using System.Reflection;
using System.Runtime.Loader;
using BuiltApp.Infrastructure;
using Umpire.Architecture;

public sealed class AppLoader : AssemblyLoadContext {
    static readonly HashSet<string> Shared = SharedClosure(typeof(Architecture).Assembly);
    readonly string bin;
    readonly AssemblyDependencyResolver? resolver;

    public AppLoader(string bin) : base("umpire-app", isCollectible: false) {
        this.bin = bin;
        var main = Directory.GetFiles(bin, "*.deps.json")
            .Select(deps => Path.Combine(bin, Path.GetFileName(deps)[..^".deps.json".Length] + ".dll"))
            .FirstOrDefault(File.Exists);
        resolver = main is null ? null : new AssemblyDependencyResolver(main);
    }

    public List<Assembly> AppAssemblies(string blockFolder, IReadOnlyList<string> skip) {
        return [.. Directory.GetFiles(bin, "*.dll")
            .Where(dll => !Shared.Contains(Path.GetFileNameWithoutExtension(dll)))
            .Where(dll => !skip.Contains(Path.GetFileNameWithoutExtension(dll), StringComparer.OrdinalIgnoreCase))
            .Where(dll => SourceFiles.HasSourceUnder(dll, blockFolder))
            .Select(LoadFromAssemblyPath)];
    }

    public IEnumerable<Assembly> ExternalAssemblies(IReadOnlyList<string> names) =>
        Directory.GetFiles(bin, "*.dll")
            .Select(dll => Path.GetFileNameWithoutExtension(dll))
            .Where(name => !Shared.Contains(name) && names.Any(e => name == e || name.StartsWith(e + ".", StringComparison.Ordinal)))
            .Select(name => LoadFromAssemblyName(new AssemblyName(name)));

    protected override Assembly? Load(AssemblyName assemblyName) {
        if (Shared.Contains(assemblyName.Name!)) {
            return null;
        }
        var path = resolver?.ResolveAssemblyToPath(assemblyName);
        if (path is null) {
            var local = Path.Combine(bin, assemblyName.Name + ".dll");
            path = File.Exists(local) ? local : null;
        }
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    static HashSet<string> SharedClosure(Assembly root) {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.GetName().Name! };
        foreach (var reference in root.GetReferencedAssemblies().Where(r => !IsFramework(r.Name!))) {
            names.UnionWith(SharedClosure(Assembly.Load(reference)));
        }
        return names;
    }

    static bool IsFramework(string name) =>
        name is "netstandard" or "mscorlib"
        || name.StartsWith("System", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal);
}
