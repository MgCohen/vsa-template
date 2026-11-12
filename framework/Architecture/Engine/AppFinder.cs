using System.Reflection;
using Mono.Cecil;

namespace VSA.Architecture.Engine;

internal static class AppFinder {
    static readonly string DefinitionName = typeof(ArchitectureDefinition).Assembly.GetName().Name!;

    static readonly string EngineName = typeof(AppFinder).Assembly.GetName().Name!;

    public static ArchitectureMap FromTestOutput(Type definition) {
        var entry = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("The test host has no entry assembly.");
        var root = entry.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == ArchitectureMap.AppRootKey)?.Value;
        return FromFolder(AppContext.BaseDirectory, definition, string.IsNullOrEmpty(root) ? null : root, entry);
    }

    public static ArchitectureMap FromFolder(string folder, Type definition, string? appRoot = null, Assembly? exclude = null) {
        appRoot = Path.GetFullPath(appRoot ?? FindGlobalJson(folder));
        var dlls = Directory.GetFiles(folder, "*.dll").Where(dll => exclude is null || !SameFile(dll, exclude.Location)).ToList();
        var warnings = new List<string>();
        var unity = UnityScriptAssemblies(appRoot, dlls, warnings);
        var tools = dlls.Where(IsArchitectureTool).ToList();
        var app = unity.Concat(dlls.Except(tools)).Where(dll => HasSourceUnder(dll, appRoot)).ToList();
        if (app.Count == 0) {
            throw new InvalidOperationException(
                $"No assembly in {folder} has source files under the app root {appRoot}. Reference the app projects from the test project.");
        }
        var definers = dlls.Where(dll => References(dll, DefinitionName));
        var searched = tools.Union(definers).Union(app).Select(Assembly.LoadFrom).Append(exclude).Append(definition.Assembly);
        var instance = FindDefinition(definition, searched);
        var externals = instance.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var external = dlls.Except(tools).Except(app).Where(dll => IsExternal(Path.GetFileNameWithoutExtension(dll), externals));
        var assemblies = app.Union(external).Select(Assembly.LoadFrom).ToList();
        return ArchitectureMap.Load(instance, assemblies, appRoot, warnings);
    }

    static List<string> UnityScriptAssemblies(string appRoot, List<string> dlls, List<string> warnings) {
        if (!UnityProject.Is(appRoot)) {
            return [];
        }
        var folder = UnityProject.ScriptAssemblies(appRoot);
        if (!Directory.Exists(folder)) {
            warnings.Add($"warning: {appRoot} is a Unity project, but it has no {UnityProject.ScriptAssembliesFolder} folder. "
                + "The checks read only the .NET build, so they do not check the code that only Unity compiles. "
                + "Open the project in Unity to compile its scripts, then run the tests again.");
            return [];
        }
        var loaded = dlls.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. Directory.GetFiles(folder, "*.dll").Where(dll => !loaded.Contains(Path.GetFileNameWithoutExtension(dll)))];
    }

    static ArchitectureDefinition FindDefinition(Type definition, IEnumerable<Assembly?> assemblies) {
        var all = assemblies.OfType<Assembly>().Distinct().SelectMany(TypesOf)
            .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .Distinct().ToList();
        var fromApp = FromApp(definition, Leaves(all.Where(typeof(IArchitectureProvider).IsAssignableFrom).ToList()));
        if (fromApp is not null) {
            return fromApp;
        }
        var leaves = Leaves(all.Where(definition.IsAssignableFrom).ToList());
        return leaves.Count switch {
            1 => (ArchitectureDefinition)Activator.CreateInstance(leaves[0])!,
            0 => throw new InvalidOperationException(
                $"No architecture definition of type {definition.FullName} found. Add a class with no constructor parameters that inherits it."),
            _ => throw new InvalidOperationException(
                $"More than one architecture definition found: {string.Join(", ", leaves.Select(t => t.FullName))}. "
                + "Keep one, or make one inherit the other."),
        };
    }

    static ArchitectureDefinition? FromApp(Type definition, List<Type> apps) {
        if (apps.Count > 1) {
            throw new InvalidOperationException(
                $"More than one app found: {string.Join(", ", apps.Select(t => t.FullName))}. Keep one app class, or make one inherit the other.");
        }
        var found = apps.Count == 1 ? ((IArchitectureProvider)Activator.CreateInstance(apps[0])!).Architecture : null;
        return definition.IsInstanceOfType(found) ? found : null;
    }

    static List<Type> Leaves(List<Type> types) => types.Where(t => !types.Any(other => other != t && t.IsAssignableFrom(other))).ToList();

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
        var name = Path.GetFileNameWithoutExtension(dll);
        return name == DefinitionName || name == EngineName || References(dll, EngineName);
    }

    static bool References(string dll, string assembly) {
        try {
            using var module = ModuleDefinition.ReadModule(dll);
            return module.AssemblyReferences.Any(r => r.Name == assembly);
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
                .Where(p => Path.IsPathRooted(p.Document.Url))
                .Select(p => Path.GetFullPath(p.Document.Url))
                .Any(file => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !UnityProject.IsInLibrary(file[prefix.Length..]));
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
