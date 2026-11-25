namespace Umpire.CheckModel;

sealed class FolderMatch {
    const string ValueStart = "{";
    readonly string[] pattern;
    readonly string[] folders;

    public FolderMatch(Part part, string[] folders) {
        Part = part;
        pattern = part.Path.Split('/');
        this.folders = folders;
        IsMatch = pattern.Length <= folders.Length && !pattern.Where((segment, i) => !Fits(segment, folders[i])).Any();
    }

    public Part Part { get; }
    public bool IsMatch { get; }
    public int Length => pattern.Length;
    public int ValueCount => pattern.Count(segment => segment.StartsWith(ValueStart, StringComparison.Ordinal));
    public bool IsUndeclaredGroupFolder => Part.IsGroup && folders.Length > pattern.Length;
    public IReadOnlyDictionary<string, string> Values => ValuesOf(Part);

    public static string[] FoldersOf(string path) {
        var segments = path.Split('/');
        return [.. segments.Take(segments.Length - 1)];
    }

    public static bool Fits(string pattern, string folder) =>
        pattern.StartsWith(ValueStart, StringComparison.Ordinal)
        || string.Equals(pattern, folder, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> ValuesOf(Part part) =>
        part.Groups.ToDictionary(group => group.Name, group => folders[group.Path.Split('/').Length - 1]);
}
