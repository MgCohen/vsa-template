namespace Umpire.CheckModel;

public sealed record FileMark(string Path, string Hash, long Modified) {
    public const string Unknown = "unknown";

    public bool IsAbsent => Hash.Length == 0;
}
