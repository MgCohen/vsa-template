namespace BuiltApp.Infrastructure;

using System.Reflection;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;

public sealed class BinReader : IDisposable {
    public const string ArchitectureKey = "Umpire.Architecture";
    public const string RootKey = "Umpire.Root";
    const string BlockBase = "Umpire.Architecture.Block`1";
    public const string DefinitionsName = "umpire-definitions";
    const string PackArchitecture = "Umpire.Kernels.PackArchitecture";
    const string KernelArchitectureBase = "Umpire.Kernels.Architecture`1";
    const string UmpirePrefix = "Umpire";
    const string ArchitectureBase = "Umpire.Architecture.Architecture";
    const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";

    readonly string bin;
    readonly string[] dlls;
    readonly IReadOnlySet<string> projects;
    readonly MetadataLoadContext context;
    readonly ReadOptions options;

    public BinReader(string bin, ReadOptions? options = null) {
        this.bin = bin;
        this.options = options ?? ReadOptions.None;
        dlls = [.. ReferencesFirst(bin, this.options.References)];
        projects = DepsFile.ProjectAssemblies(bin);
        var shared = SharedFrameworks.Folders().SelectMany(folder => Directory.GetFiles(folder, "*.dll"));
        context = new MetadataLoadContext(new PathAssemblyResolver(dlls.Concat(shared)));
    }

    public static IEnumerable<string> ReferencesFirst(string bin, IReadOnlyList<string> references) {
        var own = Dlls([bin, DefinitionsFolder(bin)]).ToList();
        var names = own.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Dlls(references).Where(dll => !names.Contains(Path.GetFileName(dll))).Concat(own);
    }

    public static string DefinitionsFolder(string bin) => Path.Combine(bin, DefinitionsName);

    static IEnumerable<string> Dlls(IEnumerable<string> folders) =>
        folders.Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*.dll"))
            .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, Source> Sources(IEnumerable<string> files, string? sourceRoot = null) {
        var sources = new Dictionary<string, Source>();
        foreach (var file in files) {
            using var symbols = new SymbolReader(file, sourceRoot);
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
        return new BinIndex(bin, [.. read.Select(r => r.Dll)], [.. read.SelectMany(r => r.Entries)]) {
            IgnoredFolders = options.IgnoredFolders,
        };
    }

    public BlockEntry Check(BlockEntry entry) =>
        entry is { Problem: null, Form: not EntryForm.App, Definition: { } type } && Problem(type) is { } problem
            ? entry.Names(type.ToString(), problem)
            : entry;

    public string? Problem(TypeName name) {
        try {
            return ProblemOf(name);
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return $"{name.FullName} does not load: {e.Message}";
        }
    }

    string? ProblemOf(TypeName name) {
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

    List<AppClass> AppClasses(Assembly assembly, Dictionary<string, Source> sources, List<UnloadedType> unloaded) {
        var found = new List<AppClass>();
        foreach (var type in Types(assembly)) {
            if (type.FullName is not { } name || !sources.TryGetValue(name, out var source)) {
                continue;
            }
            try {
                if (AppClass.KernelsOf(type) is [var kernel, ..] and var kernels) {
                    found.Add(new AppClass(Name(type), Path.GetFullPath(source.File), [.. kernels.Select(Name)],
                        [.. Bases(kernel).Select(Name)], BaseNames(type)));
                }
            } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
                unloaded.Add(new MissingTypes(context).Of(type, Path.GetFullPath(source.File), e));
            }
        }
        return found;
    }

    IEnumerable<ReadDefinition> DefinitionsOf(
        Assembly assembly, Dictionary<string, Source> sources, List<UnloadedType> unloaded) {
        foreach (var type in Types(assembly).Where(IsConcrete)) {
            ReadDefinition? found;
            try {
                found = DefinitionOf(type, sources);
            } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
                if (sources.TryGetValue(type.FullName!, out var source) && unloaded.All(u => u.Type != Name(type))) {
                    unloaded.Add(new MissingTypes(context).Of(type, Path.GetFullPath(source.File), e));
                }
                continue;
            }
            if (found is not null) {
                yield return found;
            }
        }
    }

    static ReadDefinition? DefinitionOf(Type type, Dictionary<string, Source> sources) {
        if (Base(type, ArchitectureBase) is null) {
            return null;
        }
        var file = sources.TryGetValue(type.FullName!, out var source) ? Path.GetFullPath(source.File) : null;
        var folder = file is null ? null : DefinitionType.BlockFolder(file);
        var kernelBase = Base(type, KernelArchitectureBase);
        var kernel = kernelBase is null ? null : Name(kernelBase.GetGenericArguments()[0]);
        var entry = kernelBase is null || folder is null
            ? null
            : new BlockEntry(
                folder, EntryForm.Definition, Name(kernelBase) with { FullName = PackArchitecture }, file!, Packed: Name(type));
        var blockFolder = folder is null ? null : DiskPath.Of(folder);
        return new ReadDefinition(new DefinitionType(Name(type), blockFolder, kernel, BaseNames(type)), entry);
    }

    static BlockEntry AppEntry(AppClass app) {
        var folder = ProjectFolder(Path.GetDirectoryName(app.Source)!);
        return new BlockEntry(folder, EntryForm.App, app.Type, app.Source) { HasProject = HasProject(folder) };
    }

    static bool HasProject(string folder) => Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.csproj").Any();

    static IEnumerable<BlockEntry> TypeEntries(Assembly assembly, Dictionary<string, Source> sources) {
        foreach (var type in Types(assembly).Where(IsConcrete)) {
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
                ? new BlockEntry(Path.GetDirectoryName(file)!, EntryForm.Base, Name(block.GetGenericArguments()[0]), file)
                : null;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
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
        foreach (var attribute in assembly.GetCustomAttributesData().Where(IsMetadata)) {
            if (attribute.ConstructorArguments is [{ Value: string key }, { Value: string value }]) {
                values.TryAdd(key, value);
            }
        }
        return values;
    }

    static bool IsMetadata(CustomAttributeData attribute) {
        try {
            return attribute.AttributeType.FullName == MetadataAttribute;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return false;
        }
    }

    static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }

    static bool IsConcrete(Type type) => type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false };

    static IEnumerable<Type> Bases(Type type) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            yield return b;
        }
    }

    static IReadOnlyList<string> BaseNames(Type type) =>
        [.. Bases(type).Select(b => (b.IsGenericType ? b.GetGenericTypeDefinition() : b).FullName!)];

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

    DllRead? Read(string dll) {
        SymbolReader symbols;
        try {
            symbols = new SymbolReader(dll, options.SourceRoot);
        } catch (Exception e) when (e is BadImageFormatException or IOException or InvalidOperationException) {
            return null;
        }
        using (symbols) {
            var name = Path.GetFileNameWithoutExtension(dll);
            var plain = new BinDll(dll, name, AppLoader.Shared.Contains(name), symbols.References(), [], [], [], projects.Contains(name));
            if (!symbols.HasSymbols) {
                return new DllRead(WithDefinitions(plain), []);
            }
            try {
                var assembly = context.LoadFromAssemblyPath(dll);
                var types = symbols.TypeSources().ToList();
                var sources = types.GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First().Source);
                var unloaded = new List<UnloadedType>();
                var apps = AppClasses(assembly, sources, unloaded);
                var definitions = DefinitionsOf(assembly, sources, unloaded).ToList();
                var entries = apps.Select(AppEntry).Concat(TypeEntries(assembly, sources))
                    .Concat(definitions.Select(d => d.Entry).OfType<BlockEntry>())
                    .Concat(MetadataEntries(assembly, sources)).Select(OnDisk).Select(Check).ToList();
                var read = plain with {
                    Documents = symbols.Documents(),
                    Types = types,
                    AppClasses = apps,
                    Definitions = [.. definitions.Select(d => d.Type)],
                    Unloaded = unloaded,
                };
                return new DllRead(read, entries);
            } catch (Exception e) when (e is BadImageFormatException or FileLoadException or IOException
                or TypeLoadException or InvalidOperationException) {
                return new DllRead(plain, []);
            }
        }
    }

    BinDll WithDefinitions(BinDll dll) {
        if (!dll.References.Any(r => r.StartsWith(UmpirePrefix, StringComparison.Ordinal))) {
            return dll;
        }
        try {
            var found = DefinitionsOf(context.LoadFromAssemblyPath(dll.Path), [], []).Select(d => d.Type);
            return dll with { Definitions = [.. found] };
        } catch (Exception e) when (e is BadImageFormatException or FileLoadException or IOException
            or TypeLoadException or InvalidOperationException) {
            return dll;
        }
    }

    sealed record ReadDefinition(DefinitionType Type, BlockEntry? Entry);

    sealed record DllRead(BinDll Dll, IReadOnlyList<BlockEntry> Entries);
}
