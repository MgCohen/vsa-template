namespace Umpire.CheckModel;

public sealed record Snapshot(int Format, string MadeAt, string Project, IReadOnlyList<ProjectReference> References, IReadOnlyList<UnplacedType> Unplaced,
    IReadOnlyList<string> NotCompiled, bool HasFriends, IReadOnlyList<FileFacts> Files, FileMark? Source = null) {
    public const int CurrentFormat = 2;

    public static Snapshot Of(string project, IEnumerable<FileFacts> files) => new(
        CurrentFormat, DateTime.UtcNow.ToString("o"), project, [], [], [], false, [.. files.OrderBy(f => f.Path, StringComparer.Ordinal)]);

    public static Snapshot Read(string text) {
        var snapshot = JsonReader.Read<Snapshot>(text);
        return snapshot.Format == CurrentFormat
            ? snapshot
            : throw new FormatException($"The snapshot has the format {snapshot.Format}. This tool reads {CurrentFormat}.");
    }

    public string Write() => JsonWriter.Write(this);
}
