namespace Umpire.CheckModel;

public sealed record Snapshot(int Format, string MadeAt, string Project, IReadOnlyList<ProjectReference> References, IReadOnlyList<UnplacedType> Unplaced,
    IReadOnlyList<string> NotCompiled, bool HasFriends, IReadOnlyList<FileFacts> Files, FileMark? Source = null, string? Root = null,
    string? Target = null) {
    public const int CurrentFormat = 3;

    public static Snapshot Of(string project, IEnumerable<FileFacts> files) => new(
        CurrentFormat, DateTime.UtcNow.ToString("o"), project, [], [], [], false, [.. files.OrderBy(f => f.Path, StringComparer.Ordinal)]);

    public string Write() => JsonWriter.Write(this);
}
