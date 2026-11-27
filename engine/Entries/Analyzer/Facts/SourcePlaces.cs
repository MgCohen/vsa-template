namespace UmpireEngine;

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Umpire.CheckModel;

sealed class SourcePlaces {
    const string GeneratedFolder = "/generated/";
    const string ObjectFolder = "/obj/";
    const int GeneratorFolders = 2;

    readonly string block;
    readonly string? projectFolder;
    readonly ConcurrentDictionary<SyntaxTree, string> files = new();

    public SourcePlaces(string block, string? projectFolder) {
        this.block = block;
        this.projectFolder = projectFolder;
    }

    public string FileOf(SyntaxTree tree) => files.GetOrAdd(tree, Find);

    public bool IsGenerated(SyntaxTree tree) =>
        FileOf(tree).Length == 0 || IsOutput(tree.FilePath) && Checksum(tree) is null;

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
        if (!IsOutput(path)) {
            return Path.GetFullPath(path);
        }
        return Checksum(tree) ?? (projectFolder is not null && HintOf(path) is { Length: > 0 } hint
            ? Path.GetFullPath(Path.Combine(projectFolder, hint))
            : "");
    }

    bool IsOutput(string path) => !Path.IsPathRooted(path) || BlockPath.IsInOutput(BlockPath.Of(block, path));

    string HintOf(string path) {
        var normal = "/" + (Path.IsPathRooted(path) ? BlockPath.Of(block, path) : path.Replace('\\', '/'));
        var output = normal.LastIndexOf(ObjectFolder, StringComparison.OrdinalIgnoreCase);
        var generated = output < 0 ? -1 : normal.IndexOf(GeneratedFolder, output, StringComparison.OrdinalIgnoreCase);
        if (generated < 0 && (output >= 0 || Path.IsPathRooted(path))) {
            return "";
        }
        var hint = normal.Substring(generated < 0 ? 1 : generated + GeneratedFolder.Length).Split('/').Skip(GeneratorFolders);
        return string.Join("/", hint);
    }

    static string? Checksum(SyntaxTree tree) {
        var first = tree.GetRoot().GetFirstDirective(d => d.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia));
        return first is PragmaChecksumDirectiveTriviaSyntax { File.ValueText: { } file } && Path.IsPathRooted(file)
            ? Path.GetFullPath(file)
            : null;
    }
}
