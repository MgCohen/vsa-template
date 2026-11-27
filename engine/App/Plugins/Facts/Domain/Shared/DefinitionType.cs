namespace Facts.Domain.Shared;

public sealed record DefinitionType(TypeName Type, string? Folder, TypeName? Kernel, IReadOnlyList<string> Bases) {
    public const string FolderName = "Definition";

    public DefinitionType In(Func<string, string> folderOf) =>
        Folder is { } folder ? this with { Folder = folderOf(folder) }
            : Path.GetDirectoryName(Source) is { } own && folderOf(own) is var block && block != own ? this with { Folder = block }
            : this;

    public string? Source { get; init; }

    public static bool IsFolderName(string name) => string.Equals(name, FolderName, StringComparison.OrdinalIgnoreCase);

    public static string? BlockFolder(string source) {
        for (var folder = Path.GetDirectoryName(source); folder is not null; folder = Path.GetDirectoryName(folder)) {
            if (IsFolderName(Path.GetFileName(folder))) {
                return Path.GetDirectoryName(folder);
            }
        }
        return null;
    }

    public static IReadOnlyList<DefinitionType> Leaves(IReadOnlyList<DefinitionType> definitions) =>
        [.. definitions.Where(d => !definitions.Any(other => other.Bases.Contains(d.Type.FullName)))];

    public static IReadOnlyList<DefinitionType> Roots(IReadOnlyList<DefinitionType> definitions) =>
        [.. definitions.Where(d => !definitions.Any(other => d.Bases.Contains(other.Type.FullName)))];
}
