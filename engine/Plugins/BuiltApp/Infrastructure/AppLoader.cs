namespace BuiltApp.Infrastructure;

using System.Reflection;
using System.Runtime.Loader;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.Kernel;

public sealed class AppLoader : AssemblyLoadContext {
    public static readonly IReadOnlySet<string> Shared = SharedClosure(typeof(Architecture).Assembly);
    readonly string bin;
    readonly AssemblyDependencyResolver? resolver;

    public AppLoader(string bin) : base("umpire-app", isCollectible: false) {
        this.bin = bin;
        var main = Directory.GetFiles(bin, "*.deps.json")
            .Select(deps => Path.Combine(bin, Path.GetFileName(deps)[..^".deps.json".Length] + ".dll"))
            .FirstOrDefault(File.Exists);
        resolver = main is null ? null : new AssemblyDependencyResolver(main);
    }

    public Architecture CreateArchitecture(TypeName name) => (Architecture)Create(name);

    public Architecture ArchitectureOfApp(TypeName appClass) {
        var app = Create(appClass);
        var pack = app.GetType().GetProperty(nameof(App.Pack))!.GetValue(app)!;
        return (Architecture)pack.GetType().GetProperty(nameof(Pack.Architecture))!.GetValue(pack)!;
    }

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
        foreach (var reference in root.GetReferencedAssemblies().Where(r => !BinDll.IsSystem(r.Name!))) {
            names.UnionWith(SharedClosure(Assembly.Load(reference)));
        }
        return names;
    }

    object Create(TypeName name) {
        var type = LoadFromAssemblyName(new AssemblyName(name.Assembly)).GetType(name.FullName, throwOnError: true)!;
        return Activator.CreateInstance(type)!;
    }
}
