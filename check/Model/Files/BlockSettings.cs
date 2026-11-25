namespace Umpire.CheckModel;

public sealed record BlockSettings(
    int Format,
    string Block,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> InnerBlocks,
    IReadOnlyList<string> OtherStates,
    IReadOnlyList<string> IgnoredFolders,
    IReadOnlyList<FolderAlias> FolderAliases,
    IReadOnlyDictionary<string, string> Severities) {
    public const int CurrentFormat = 1;

    public static BlockSettings Of(string block, params string[] projects) =>
        new(CurrentFormat, block, projects, [], [], [], [], new Dictionary<string, string>());

    public static BlockSettings Read(string text) {
        var settings = JsonReader.Read<BlockSettings>(text);
        return settings.Format == CurrentFormat
            ? settings
            : throw new FormatException($"The block file has the format {settings.Format}. This tool reads {CurrentFormat}.");
    }

    public string Write() => JsonWriter.Write(this);

    public Severity? SeverityOf(string code, Severity usual) => !Severities.TryGetValue(code, out var text)
        ? usual
        : text.Trim().ToLowerInvariant() switch {
            "none" or "silent" => null,
            "error" => Severity.Error,
            "warning" => Severity.Warning,
            "suggestion" or "info" => Severity.Info,
            _ => usual,
        };
}
