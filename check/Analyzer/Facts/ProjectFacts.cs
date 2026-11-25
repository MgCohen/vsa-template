namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public sealed class ProjectFacts {
    readonly Compilation compilation;
    readonly string block;
    readonly SourcePlaces places;
    readonly Dictionary<TypeName, Lazy<IReadOnlyList<AppReference>>> references = [];

    public ProjectFacts(Compilation compilation, string block, string? projectFolder = null) {
        this.compilation = compilation;
        this.block = block;
        places = new SourcePlaces(block, projectFolder ?? FolderOf(compilation, block));
        var types = new List<TypeFile>();
        foreach (var type in Symbols.TypesOf(compilation.Assembly.GlobalNamespace)) {
            types.AddRange(Declarations(type));
            references[Symbols.NameOf(type)] = new(() => ReferenceReader.Read(type, places, tree => compilation.GetSemanticModel(tree)));
        }
        var byFile = types.ToLookup(t => t.File, t => t.Facts);
        Declared = Snapshot.Of(Name, [.. Trees().Select(tree => FactsOf(tree, byFile))]);
    }

    public string Name => compilation.AssemblyName ?? "";

    public Snapshot Declared { get; }

    public CurrentProject Current => new(Declared, References);

    public IReadOnlyList<AppReference> References(TypeName type) => references.TryGetValue(type, out var found) ? found.Value : [];

    public Snapshot Full() => Declared with {
        Files = [.. Declared.Files.Select(file => Dated(file, BlockPath.In(block, file.Path)) with {
            Types = [.. file.Types.Select(type => type with {
                References = [.. References(type.Name).Where(r => BlockPath.Of(block, r.File) == file.Path).Select(r => r.Fact)],
            })],
        })],
    };

    public Location LocationOf(Finding finding) {
        var tree = compilation.SyntaxTrees.FirstOrDefault(t => IsOwnFile(t, finding.File));
        var line = new LinePosition(finding.Line - 1, finding.Column - 1);
        if (tree is not null && line.Line < tree.GetText().Lines.Count && line.Character <= tree.GetText().Lines[line.Line].Span.Length) {
            return tree.GetRoot().FindToken(tree.GetText().Lines.GetPosition(line)).GetLocation();
        }
        return finding.File is { } file ? Location.Create(file, default, new LinePositionSpan(line, line)) : Location.None;
    }

    static string? FolderOf(Compilation compilation, string block) {
        var folders = compilation.SyntaxTrees.Select(t => t.FilePath).Where(f => Path.IsPathRooted(f) && !BlockPath.IsInOutput(BlockPath.Of(block, f)))
            .Select(f => Path.GetDirectoryName(f)!).ToList();
        var common = folders.OrderBy(f => f.Length).FirstOrDefault();
        while (common is not null && !folders.All(f => f == common || BlockPath.IsUnder(common, f))) {
            common = Path.GetDirectoryName(common);
        }
        return common;
    }

    static bool IsOwnFile(SyntaxTree tree, string? file) =>
        tree.FilePath.Length > 0 && string.Equals(Path.GetFullPath(tree.FilePath), file, StringComparison.OrdinalIgnoreCase);

    static string Hash(SyntaxTree tree) {
        var text = tree.GetText();
        return text.ChecksumAlgorithm == SourceHashAlgorithm.Sha256
            ? BitConverter.ToString([.. text.GetChecksum()]).Replace("-", "").ToLowerInvariant()
            : FileHash.Of(tree.FilePath);
    }

    static FileFacts Dated(FileFacts file, string path) {
        if (file.Hash.Length == 0) {
            return file;
        }
        var modified = File.GetLastWriteTimeUtc(path).Ticks;
        return file with { Modified = FileHash.Of(path) == file.Hash ? modified : 0 };
    }

    IEnumerable<SyntaxTree> Trees() => compilation.SyntaxTrees
        .Where(tree => places.FileOf(tree) is { Length: > 0 } file && !BlockPath.IsInOutput(BlockPath.Of(block, file)))
        .GroupBy(places.FileOf, StringComparer.OrdinalIgnoreCase).Select(group => group.OrderByDescending(t => IsOwnFile(t, group.Key)).First());

    FileFacts FactsOf(SyntaxTree tree, ILookup<string, TypeFacts> byFile) {
        var file = places.FileOf(tree);
        var hash = !File.Exists(file) ? "" : IsOwnFile(tree, file) ? Hash(tree) : FileHash.Of(file);
        return new FileFacts(BlockPath.Of(block, file), hash, 0, [.. byFile[file]]);
    }

    IEnumerable<TypeFile> Declarations(INamedTypeSymbol type) {
        var files = type.Locations.Where(l => l.SourceTree is not null)
            .GroupBy(l => places.FileOf(l.SourceTree!)).Where(g => g.Key.Length > 0)
            .OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        var content = new ContentReader(type, places);
        foreach (var file in files) {
            var position = places.PositionOf(file.First());
            yield return new TypeFile(file.Key, new TypeFacts(
                Symbols.NameOf(type),
                new TypeLocation(
                    type.ContainingNamespace is { IsGlobalNamespace: false } space ? space.ToDisplayString() : "",
                    position.Line, position.Column, type.ContainingType is { } outer ? Symbols.NameOf(outer).FullName : null),
                content.Read(file.Key, file == files[0]), []));
        }
    }
}
