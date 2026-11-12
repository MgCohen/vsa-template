using System.Reflection;
using Mono.Cecil;

namespace VSA.Architecture.Engine;

public sealed record DiscoveredApp(string AppRoot, ArchitectureDefinition Definition, IReadOnlyList<Assembly> Assemblies) {
    public const string AppRootKey = "VsaAppRoot";

    static readonly string EngineName = typeof(ArchitectureDefinition).Assembly.GetName().Name!;

    public static DiscoveredApp FromTestOutput(Type definition) {
        var entry = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("The test host has no entry assembly.");
        var root = entry.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == AppRootKey)?.Value;
        return FromFolder(AppContext.BaseDirectory, definition, string.IsNullOrEmpty(root) ? null : root, entry);
    }

    public static DiscoveredApp FromFolder(string folder, Type definition, string? appRoot = null, Assembly? exclude = null) {
        appRoot = Path.GetFullPath(appRoot ?? FindGlobalJson(folder));
        var dlls = Directory.GetFiles(folder, "*.dll").Where(dll => exclude is null || !SameFile(dll, exclude.Location)).ToList();
        var tools = dlls.Where(IsArchitectureTool).ToList();
        var instance = FindDefinition(definition, tools.Select(Assembly.LoadFrom).Append(exclude).Append(definition.Assembly));
        var externals = instance.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var candidates = dlls.Except(tools).ToList();
        var app = candidates.Where(dll => HasSourceUnder(dll, appRoot)).ToList();
        if (app.Count == 0) {
            throw new InvalidOperationException(
                $"No assembly in {folder} has source files under the app root {appRoot}. Reference the app projects from the test project.");
        }
        var external = candidates.Where(dll => IsExternal(Path.GetFileNameWithoutExtension(dll), externals));
        var assemblies = app.Union(external).Select(Assembly.LoadFrom).ToList();
        return new DiscoveredApp(appRoot, instance, assemblies);
    }

    static ArchitectureDefinition FindDefinition(Type definition, IEnumerable<Assembly?> assemblies) {
        var all = assemblies.OfType<Assembly>().Distinct().SelectMany(TypesOf)
            .Where(t => !t.IsAbstract && definition.IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null)
            .Distinct().ToList();
        var leaves = all.Where(t => !all.Any(other => other != t && t.IsAssignableFrom(other))).ToList();
        return leaves.Count switch {
            1 => (ArchitectureDefinition)Activator.CreateInstance(leaves[0])!,
            0 => throw new InvalidOperationException(
                $"No architecture definition of type {definition.FullName} found. Add a class with no constructor parameters that inherits it."),
            _ => throw new InvalidOperationException(
                $"More than one architecture definition found: {string.Join(", ", leaves.Select(t => t.FullName))}. "
                + "Keep one, or make one inherit the other."),
        };
    }

    static IEnumerable<Type> TypesOf(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    static bool IsExternal(string name, IReadOnlyList<string> externals) =>
        externals.Any(e => name == e || name.StartsWith(e + ".", StringComparison.Ordinal));

    static bool IsArchitectureTool(string dll) {
        if (Path.GetFileNameWithoutExtension(dll) == EngineName) {
            return true;
        }
        try {
            using var module = ModuleDefinition.ReadModule(dll);
            return module.AssemblyReferences.Any(r => r.Name == EngineName);
        } catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or IOException) {
            return false;
        }
    }

    static bool HasSourceUnder(string dll, string appRoot) {
        if (!SourceFiles.HasSymbols(dll)) {
            return false;
        }
        try {
            using var module = ModuleDefinition.ReadModule(dll, new ReaderParameters { ReadSymbols = true });
            var prefix = Path.TrimEndingDirectorySeparator(appRoot) + Path.DirectorySeparatorChar;
            return module.Types.SelectMany(t => t.Methods).Where(m => m.HasBody)
                .SelectMany(m => m.DebugInformation.SequencePoints)
                .Any(p => Path.IsPathRooted(p.Document.Url) && Path.GetFullPath(p.Document.Url).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        } catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or IOException) {
            return false;
        }
    }

    static bool SameFile(string a, string b) => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    static string FindGlobalJson(string start) {
        for (var folder = Path.GetFullPath(start); folder is not null; folder = Path.GetDirectoryName(folder)) {
            if (File.Exists(Path.Combine(folder, "global.json"))) {
                return folder;
            }
        }
        throw new InvalidOperationException($"No global.json above {start}. Add global.json to the app root.");
    }
}
