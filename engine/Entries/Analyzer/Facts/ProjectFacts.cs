namespace UmpireEngine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Model.Infrastructure;
using Umpire.CheckModel;

public sealed class ProjectFacts : IModelFacts {
    readonly Compilation compilation;
    readonly string block;
    readonly SourcePlaces places;
    readonly string? folder;
    readonly Dictionary<TypeName, Lazy<IReadOnlyList<AppReference>>> references = [];

    public ProjectFacts(Compilation compilation, string block, string? projectFolder = null) {
        this.compilation = compilation;
        this.block = block;
        folder = projectFolder ?? FolderOf(compilation, block);
        places = new SourcePlaces(block, folder);
        var types = new List<TypeFile>();
        foreach (var type in Symbols.TypesOf(compilation.Assembly.GlobalNamespace)) {
            types.AddRange(Declarations(type));
            references[Symbols.NameOf(type)] = new(() => ReferenceReader.Read(type, places, tree => compilation.GetSemanticModel(tree)));
        }
        var byFile = types.ToLookup(t => t.File, t => t.Facts);
        Declared = Snapshot.Of(Name, [.. Trees().Select(tree => FactsOf(tree, byFile))]) with {
            References = [.. compilation.ReferencedAssemblyNames.Select(a => a.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal)
                .Select(name => new ProjectReference(name, ""))],
            Unplaced = [.. Unplaced(types).OrderBy(u => u.Name, StringComparer.Ordinal)],
            HasFriends = HasFriends,
        };
    }

    public string Name => compilation.AssemblyName ?? "";

    public string? Folder => folder;

    public Snapshot Declared { get; }

    public CurrentProject Current => new(Declared, References);

    public static Location LocationOf(Compilation compilation, string? file, int lineNumber, int column) {
        var tree = compilation.SyntaxTrees.FirstOrDefault(t => IsOwnFile(t, file));
        var line = new LinePosition(lineNumber - 1, column - 1);
        if (tree is not null && line.Line < tree.GetText().Lines.Count && line.Character <= tree.GetText().Lines[line.Line].Span.Length) {
            return tree.GetRoot().FindToken(tree.GetText().Lines.GetPosition(line)).GetLocation();
        }
        return file is { } path ? Location.Create(path, default, new LinePositionSpan(line, line)) : Location.None;
    }

    public IReadOnlyList<AppReference> References(TypeName type) => references.TryGetValue(type, out var found) ? found.Value : [];

    public Snapshot Full() => Declared with {
        Files = [.. Declared.Files.Select(file => file with {
            Types = [.. file.Types.Select(type => type with {
                References = [.. References(type.Name).Where(r => BlockPath.Of(block, r.File) == file.Path).Select(r => r.Fact)],
            })],
        })],
    };

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

    bool HasFriends => compilation.Assembly.GetAttributes()
        .Any(a => a.AttributeClass?.Name == "InternalsVisibleToAttribute");

    bool HasNoFilePart(INamedTypeSymbol type) =>
        type.Locations.Any(l => l.SourceTree is null || places.FileOf(l.SourceTree).Length == 0);

    IEnumerable<UnplacedType> Unplaced(List<TypeFile> types) {
        var placed = new HashSet<TypeName>(types.Select(t => t.Facts.Name));
        return Symbols.TypesOf(compilation.Assembly.GlobalNamespace).Where(HasNoFilePart).Select(t => {
            var name = Symbols.NameOf(t);
            var facts = new TypeFacts(name, new TypeLocation("", 0, 0), new ContentReader(t, places).Read("", !placed.Contains(name)), []);
            return new UnplacedType(name.FullName, Surface.Of(facts, HasFriends));
        });
    }

    IEnumerable<SyntaxTree> Trees() => compilation.SyntaxTrees
        .Where(tree => places.FileOf(tree) is { Length: > 0 } file && !BlockPath.IsInOutput(BlockPath.Of(block, file)))
        .GroupBy(places.FileOf, StringComparer.OrdinalIgnoreCase).Select(group => group.OrderByDescending(t => IsOwnFile(t, group.Key)).First());

    FileFacts FactsOf(SyntaxTree tree, ILookup<string, TypeFacts> byFile) {
        var file = places.FileOf(tree);
        var hash = !File.Exists(file) ? "" : IsOwnFile(tree, file) ? Hash(tree) : FileHash.Of(file);
        return new FileFacts(BlockPath.Of(block, file), hash, 0, places.IsGenerated(tree), [.. byFile[file]]);
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
