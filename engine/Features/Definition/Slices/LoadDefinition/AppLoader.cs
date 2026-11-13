using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Report.Domain.Shared;
using VSA.Architecture;

namespace Definition.Slices.LoadDefinition;

sealed class AppLoader : AssemblyLoadContext {
    static readonly HashSet<string> Shared = SharedClosure(typeof(ArchitectureDefinition).Assembly);
    readonly string bin;
    readonly AssemblyDependencyResolver? resolver;

    public AppLoader(string bin) : base("vsa-app", isCollectible: false) {
        this.bin = bin;
        var main = Directory.GetFiles(bin, "*.deps.json")
            .Select(deps => Path.Combine(bin, Path.GetFileName(deps)[..^".deps.json".Length] + ".dll"))
            .FirstOrDefault(File.Exists);
        resolver = main is null ? null : new AssemblyDependencyResolver(main);
    }

    public IEnumerable<Finding> VersionFindings() {
        foreach (var name in Shared.Order(StringComparer.Ordinal)) {
            var dll = Path.Combine(bin, name + ".dll");
            var engineCopy = Path.Combine(AppContext.BaseDirectory, name + ".dll");
            if (!File.Exists(dll) || !File.Exists(engineCopy)) {
                continue;
            }
            var app = PackageVersion(dll);
            var engine = PackageVersion(engineCopy);
            if (app != engine) {
                yield return new Finding("VSA3001", FindingSeverity.Error,
                    $"The app uses {name} {app}, but the engine uses {engine}. Use one version: run dotnet vsa update.", dll, 1);
            }
        }
    }

    public List<Assembly> AppAssemblies(string appRoot) {
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(appRoot)) + Path.DirectorySeparatorChar;
        return [.. Directory.GetFiles(bin, "*.dll")
            .Where(dll => !Shared.Contains(Path.GetFileNameWithoutExtension(dll)) && HasSourceUnder(dll, prefix))
            .Select(LoadFromAssemblyPath)];
    }

    public IEnumerable<Assembly> ExternalAssemblies(IReadOnlyList<string> names) =>
        Directory.GetFiles(bin, "*.dll")
            .Select(dll => Path.GetFileNameWithoutExtension(dll))
            .Where(name => !Shared.Contains(name) && names.Any(e => name == e || name.StartsWith(e + ".", StringComparison.Ordinal)))
            .Select(name => LoadFromAssemblyName(new AssemblyName(name)));

    protected override Assembly? Load(AssemblyName name) {
        if (Shared.Contains(name.Name!)) {
            return null;
        }
        var path = resolver?.ResolveAssemblyToPath(name);
        if (path is null) {
            var local = Path.Combine(bin, name.Name + ".dll");
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

    static string PackageVersion(string dll) {
        var version = FileVersionInfo.GetVersionInfo(dll).ProductVersion ?? "";
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? version : version[..plus];
    }

    static bool HasSourceUnder(string dll, string prefix) {
        if (!File.Exists(Path.ChangeExtension(dll, ".pdb"))) {
            return false;
        }
        try {
            using var module = ModuleDefinition.ReadModule(dll, new ReaderParameters { ReadSymbols = true });
            return module.Types.SelectMany(t => t.Methods).Where(m => m.HasBody)
                .SelectMany(m => m.DebugInformation.SequencePoints)
                .Select(p => p.Document.Url)
                .Where(Path.IsPathRooted)
                .Any(url => Path.GetFullPath(url).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        } catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or IOException) {
            return false;
        }
    }
}
