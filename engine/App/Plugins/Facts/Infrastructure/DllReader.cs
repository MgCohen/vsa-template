namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared.Dll;
using Umpire.Utility;

sealed class DllReader : IDisposable {
    const string EmbeddedAttribute = "Microsoft.CodeAnalysis.EmbeddedAttribute";
    const string ModuleType = "<Module>";

    readonly MetadataLoadContext context;
    readonly string? sourceRoot;

    public DllReader(IEnumerable<string> resolve, string? sourceRoot) {
        this.sourceRoot = sourceRoot;
        var shared = SharedFrameworks.Folders().SelectMany(folder => Directory.GetFiles(folder, "*.dll"));
        var given = resolve.Where(File.Exists).ToList();
        var siblings = given.Select(Path.GetDirectoryName).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(folder => Directory.GetFiles(folder, "*.dll"));
        var files = given.Concat(siblings).Concat(shared).DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        context = new MetadataLoadContext(new PathAssemblyResolver(files));
    }

    public DllFacts? Read(string dll) {
        SymbolReader symbols;
        try {
            symbols = new SymbolReader(dll, sourceRoot);
        } catch (Exception e) when (IsLoadProblem(e)) {
            return null;
        }
        using (symbols) {
            if (!symbols.HasSymbols || !symbols.MatchesImage) {
                return null;
            }
            try {
                return Read(dll, symbols);
            } catch (Exception e) when (IsLoadProblem(e)) {
                return null;
            }
        }
    }

    public void Dispose() => context.Dispose();

    DllFacts Read(string dll, SymbolReader symbols) {
        var assembly = context.LoadFromAssemblyPath(dll);
        var name = assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(dll);
        var names = new MetadataNames(symbols.Metadata, context, name);
        var dllContext = new DllContext(symbols, names, new BodyUses(symbols, names));
        var sources = symbols.TypeSources().GroupBy(t => t.FullName).ToDictionary(g => g.Key, g => g.First().Source);
        var types = new List<DllType>();
        foreach (var type in Types(assembly)) {
            var fullName = ReflectionShapes.FullName(type);
            if (IsUserType(type, fullName) && sources.TryGetValue(fullName, out var source) && Read(type, dllContext, source) is { } read) {
                types.Add(read);
            }
        }
        var files = symbols.SourceDocuments().Where(d => !BuildOutput.Contains(d.File)).Select(d => new DllSourceFile(d.File, d.Hash));
        return new DllFacts(name, dll, [.. files], types);
    }

    static DllType? Read(Type type, DllContext context, Umpire.Architecture.Source source) {
        try {
            return new DllTypeReader(type, context, source).Read();
        } catch (Exception e) when (IsLoadProblem(e)) {
            return null;
        }
    }

    static bool IsLoadProblem(Exception e) =>
        e is BadImageFormatException or FileLoadException or IOException or TypeLoadException or NotSupportedException
            or UnauthorizedAccessException or InvalidOperationException or ArgumentException;

    static bool IsUserType(Type type, string fullName) =>
        fullName != ModuleType && !fullName.Contains("<") && !ReflectionFlags.Has(type, EmbeddedAttribute);

    static IEnumerable<Type> Types(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch (ReflectionTypeLoadException e) {
            return e.Types.OfType<Type>();
        }
    }
}
