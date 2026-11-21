namespace BuiltApp.Infrastructure;

using System.Reflection;
using System.Runtime.InteropServices;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;

public sealed class BinReader : IDisposable {
    public const string ArchitectureKey = "Umpire.Architecture";
    public const string RootKey = "Umpire.Root";
    const string BlockBase = "Umpire.Architecture.Block`1";
    const string ArchitectureBase = "Umpire.Architecture.Architecture";
    const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";
    const string AspNetCore = "Microsoft.AspNetCore.App";

    readonly string bin;
    readonly string[] dlls;
    readonly IReadOnlySet<string> projects;
    readonly MetadataLoadContext context;

    public BinReader(string bin) {
        this.bin = bin;
        dlls = Directory.Exists(bin) ? Directory.GetFiles(bin, "*.dll") : [];
        projects = DepsFile.ProjectAssemblies(bin);
        var shared = SharedFrameworks().SelectMany(folder => Directory.GetFiles(folder, "*.dll"));
        context = new MetadataLoadContext(new PathAssemblyResolver(dlls.Concat(shared)));
    }

    public static IReadOnlyDictionary<string, Source> Sources(IEnumerable<string> files) {
        var sources = new Dictionary<string, Source>();
        foreach (var file in files) {
            using var symbols = new SymbolReader(file);
            foreach (var type in symbols.TypeSources()) {
                sources.TryAdd(type.FullName, type.Source);
            }
        }
        return sources;
    }

    public static string? SourceFolder(IEnumerable<Source> sources) {
        var files = sources.Select(s => Path.GetFullPath(s.File)).ToList();
        if (CommonFolder(files) is not { } all) {
            return null;
        }
        return CommonFolder([.. files.Where(file => !IsGenerated(Path.GetRelativePath(all, file)))]) ?? all;
    }

    public BinIndex Index() {
        var read = dlls.Select(Read).OfType<DllRead>().ToList();
        return new BinIndex(bin, [.. read.Select(r => r.Dll)], [.. read.SelectMany(r => r.Entries)]);
    }

    public BlockEntry Check(BlockEntry entry) =>
        entry is { Problem: null, Form: not EntryForm.App, Type: { } type } && Problem(type) is { } problem ? entry.Names(type.ToString(), problem) : entry;

    public string? Problem(TypeName name) {
        Assembly assembly;
        try {
            assembly = context.LoadFromAssemblyName(name.Assembly);
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException) {
            return $"the build output has no assembly {name.Assembly}";
        }
        var type = assembly.GetType(name.FullName);
        if (type is null) {
            return $"{name.Assembly} has no type {name.FullName}";
        }
        if (Base(type, ArchitectureBase) is null) {
            return $"{name.FullName} does not inherit {ArchitectureBase}";
        }
        if (type.IsAbstract) {
            return $"{name.FullName} is abstract";
        }
        return type.GetConstructor(Type.EmptyTypes) is null ? $"{name.FullName} has no public constructor with no parameters" : null;
    }

    public void Dispose() => context.Dispose();

    static List<AppClass> AppClasses(Assembly assembly, Dictionary<string, Source> sources) {
        var found = new List<AppClass>();
        foreach (var type in Types(assembly)) {
            if (type.FullName is not { } name || !sources.TryGetValue(name, out var source)) {
                continue;
            }
            try {
                if (AppClass.PacksOf(type) is [var pack, ..] and var packs) {
                    var architecture = AppClass.ArchitectureOf(pack) is { } architectureType ? Name(architectureType) : null;
                    found.Add(new AppClass(Name(type), Path.GetFullPath(source.File), [.. packs.Select(Name)], architecture,
                        AppClass.BasesOf(type)));
                }
            } catch (Exception e) when (e is FileNotFoundException or TypeLoadException) {
                continue;
            }
        }
        return found;
    }

    static BlockEntry AppEntry(AppClass app) =>
        new(ProjectFolder(Path.GetDirectoryName(app.Source)!), EntryForm.App, app.Type, app.Source);

    static IEnumerable<BlockEntry> TypeEntries(Assembly assembly, Dictionary<string, Source> sources) {
        foreach (var type in Types(assembly).Where(t => t is { IsAbstract: false, IsInterface: false })) {
            if (type.FullName is not { } name || !sources.TryGetValue(name, out var source)) {
                continue;
            }
            if (BlockEntry(type, source) is { } entry) {
                yield return entry;
            }
        }
    }

    static BlockEntry? BlockEntry(Type type, Source source) {
        var file = Path.GetFullPath(source.File);
        try {
            return Base(type, BlockBase) is { } block
                ? new BlockEntry(Path.GetDirectoryName(file)!, EntryForm.Base, Name(block.GetGenericArguments()[0]), file,
                    Packed: Packed(type, block))
                : null;
        } catch (Exception e) when (e is FileNotFoundException or TypeLoadException) {
            return null;
        }
    }

    static IEnumerable<BlockEntry> MetadataEntries(Assembly assembly, Dictionary<string, Source> sources) {
        var values = Metadata(assembly);
        if (!values.TryGetValue(ArchitectureKey, out var architecture) || SourceFolder(sources.Values) is not { } common) {
            yield break;
        }
        var project = ProjectFolder(common);
        var folder = values.TryGetValue(RootKey, out var root) ? Path.GetFullPath(Path.Combine(project, root)) : project;
        var file = Directory.EnumerateFiles(project, "*.csproj").FirstOrDefault() ?? project;
        var type = TypeName.Parse(architecture);
        var entry = new BlockEntry(Path.TrimEndingDirectorySeparator(folder), EntryForm.Metadata, type, file);
        yield return type is null ? entry.Names(architecture, TypeName.NoAssembly) : entry;
    }

    static Dictionary<string, string> Metadata(Assembly assembly) {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in assembly.GetCustomAttributesData().Where(a => a.AttributeType.FullName == MetadataAttribute)) {
            if (attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }]) {
                values.TryAdd(key, value);
            }
        }
        return values;
    }

    static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    static TypeName? Packed(Type type, Type block) {
        for (var b = type.BaseType; b is not null && b != block; b = b.BaseType) {
            var packed = b.IsGenericType
                ? b.GetGenericArguments().FirstOrDefault(a => !a.IsGenericParameter && Base(a, ArchitectureBase) is not null)
                : null;
            if (packed is not null) {
                return Name(packed);
            }
        }
        return null;
    }

    static Type? Base(Type type, string name) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            var definition = b.IsGenericType ? b.GetGenericTypeDefinition() : b;
            if (definition.FullName == name) {
                return b;
            }
        }
        return null;
    }

    static BlockEntry OnDisk(BlockEntry entry) =>
        entry with { Folder = DiskPath.Of(entry.Folder), Source = DiskPath.Of(entry.Source) };

    static TypeName Name(Type type) => new(type.FullName!, type.Assembly.GetName().Name!);

    static string? CommonFolder(IReadOnlyList<string> files) {
        var folders = files.Select(file => Path.GetDirectoryName(file)!.Split(Path.DirectorySeparatorChar)).ToList();
        if (folders.Count == 0) {
            return null;
        }
        var common = Enumerable.Range(0, folders.Min(f => f.Length))
            .TakeWhile(i => folders.All(f => string.Equals(f[i], folders[0][i], StringComparison.OrdinalIgnoreCase)))
            .Count();
        return common == 0 ? null : string.Join(Path.DirectorySeparatorChar, folders[0][..common]);
    }

    static bool IsGenerated(string relative) =>
        relative.Split('/', '\\')[..^1].Any(BuildOutput.IsIgnored);

    static string ProjectFolder(string start) {
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent) {
            if (folder.Exists && folder.EnumerateFiles("*.csproj").Any()) {
                return folder.FullName;
            }
        }
        return start;
    }

    static IEnumerable<string> SharedFrameworks() {
        var runtime = RuntimeEnvironment.GetRuntimeDirectory();
        var shared = Path.GetDirectoryName(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(runtime)));
        var web = shared is null ? null : Path.Combine(shared, AspNetCore);
        var newest = web is not null && Directory.Exists(web)
            ? Directory.GetDirectories(web).OrderByDescending(VersionOf).FirstOrDefault()
            : null;
        return newest is null ? [runtime] : [runtime, newest];
    }

    static Version VersionOf(string folder) => Version.TryParse(Path.GetFileName(folder), out var version) ? version : new Version();

    DllRead? Read(string dll) {
        SymbolReader symbols;
        try {
            symbols = new SymbolReader(dll);
        } catch (Exception e) when (e is BadImageFormatException or IOException or InvalidOperationException) {
            return null;
        }
        using (symbols) {
            var name = Path.GetFileNameWithoutExtension(dll);
            var plain = new BinDll(dll, name, AppLoader.Shared.Contains(name), symbols.References(), [], [], [], projects.Contains(name));
            if (!symbols.HasSymbols) {
                return new DllRead(plain, []);
            }
            try {
                var assembly = context.LoadFromAssemblyPath(dll);
                var types = symbols.TypeSources().ToList();
                var sources = types.GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First().Source);
                var apps = AppClasses(assembly, sources);
                var entries = apps.Select(AppEntry).Concat(TypeEntries(assembly, sources))
                    .Concat(MetadataEntries(assembly, sources)).Select(OnDisk).Select(Check).ToList();
                return new DllRead(plain with { Documents = symbols.Documents(), Types = types, AppClasses = apps }, entries);
            } catch (Exception e) when (e is BadImageFormatException or FileLoadException or IOException
                or TypeLoadException or InvalidOperationException) {
                return new DllRead(plain, []);
            }
        }
    }

    sealed record DllRead(BinDll Dll, IReadOnlyList<BlockEntry> Entries);
}
