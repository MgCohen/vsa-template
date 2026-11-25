namespace Umpire.CheckModel.Analyzer;

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

sealed class SourcePlaces {
    const string GeneratedFolder = "/generated/";
    const int GeneratorFolders = 2;

    readonly string block;
    readonly string? projectFolder;
    readonly ConcurrentDictionary<SyntaxTree, string> files = new();

    public SourcePlaces(string block, string? projectFolder) {
        this.block = block;
        this.projectFolder = projectFolder;
    }

    public string FileOf(SyntaxTree tree) => files.GetOrAdd(tree, Find);

    public Position PositionOf(Location location) {
        var span = location.GetMappedLineSpan();
        var isMapped = span.HasMappedPath && SameFile(span.Path, location.SourceTree);
        var isOwn = location.SourceTree is { } tree && tree.FilePath.Length > 0 && SameFile(tree.FilePath, tree);
        if (!isMapped && !isOwn) {
            return new Position(1, 1);
        }
        var start = isMapped ? span.StartLinePosition : location.GetLineSpan().StartLinePosition;
        return new Position(start.Line + 1, start.Character + 1);
    }

    bool SameFile(string path, SyntaxTree? tree) =>
        tree is not null && path.Length > 0 && string.Equals(Full(path, tree), FileOf(tree), StringComparison.OrdinalIgnoreCase);

    static string Full(string path, SyntaxTree tree) =>
        Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tree.FilePath) ?? "", path));

    string Find(SyntaxTree tree) {
        var path = tree.FilePath;
        if (path.Length == 0) {
            return "";
        }
        if (Path.IsPathRooted(path) && !BlockPath.IsInOutput(BlockPath.Of(block, path))) {
            return Path.GetFullPath(path);
        }
        if (Checksum(tree) is { } source) {
            return source;
        }
        var normal = "/" + path.Replace('\\', '/');
        var generated = normal.LastIndexOf(GeneratedFolder, StringComparison.OrdinalIgnoreCase);
        var hint = normal.Substring(generated < 0 ? 1 : generated + GeneratedFolder.Length).Split('/').Skip(GeneratorFolders).ToList();
        return projectFolder is null || hint.Count == 0 || generated < 0 && Path.IsPathRooted(path)
            ? ""
            : Path.GetFullPath(Path.Combine(projectFolder, string.Join("/", hint)));
    }

    static string? Checksum(SyntaxTree tree) {
        var first = tree.GetRoot().GetFirstDirective(d => d.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia));
        return first is PragmaChecksumDirectiveTriviaSyntax { File.ValueText: { } file } && Path.IsPathRooted(file)
            ? Path.GetFullPath(file)
            : null;
    }
}
