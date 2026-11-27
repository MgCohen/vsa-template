namespace Umpire.CheckModel;

public sealed record BlockSettings(
    int Format,
    string Block,
    CheckMode Mode,
    string DefinitionHash,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> Libraries,
    IReadOnlyList<string> InnerBlocks,
    IReadOnlyList<string> OtherStates,
    IReadOnlyList<string> IgnoredFolders,
    IReadOnlyList<FolderAlias> FolderAliases,
    IReadOnlyList<FileMark> DefinitionSources,
    IReadOnlyList<FileMark> Inputs,
    IReadOnlyDictionary<string, string> Severities,
    IReadOnlyList<SnapshotPlace> Snapshots) {
    public const int CurrentFormat = 3;

    public IReadOnlyList<string> Targets => [.. Snapshots
        .GroupBy(s => s.Project, StringComparer.OrdinalIgnoreCase).Where(project => project.Count() > 1).SelectMany(project => project)
        .Select(s => s.Target).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.Ordinal)];

    public static BlockSettings Of(string block, params string[] projects) => new(
        CurrentFormat, block, CheckModes.Default, "", projects, [], [], [], [], [], [], [], new Dictionary<string, string>(), []);

    public string? SnapshotOf(string project, string? target) {
        var places = Snapshots.Where(s => string.Equals(s.Project, project, StringComparison.OrdinalIgnoreCase)).ToList();
        return (places.FirstOrDefault(s => target is not null && string.Equals(s.Target, target, StringComparison.OrdinalIgnoreCase))
            ?? places.FirstOrDefault())?.File;
    }

    public static BlockSettings Read(string text) {
        var settings = JsonReader.Read<BlockSettings>(text);
        return settings.Format == CurrentFormat
            ? settings
            : throw new FormatException($"The block file has the format {settings.Format}. This tool reads {CurrentFormat}.");
    }

    public string Write() => JsonWriter.Write(this);

    public Severity? SeverityOf(string code, Severity usual) => (Configured(code, usual), Mode) switch {
        (Severity.Error, CheckMode.Warn) => Severity.Warning,
        (var severity, _) => severity,
    };

    Severity? Configured(string code, Severity usual) => !Severities.TryGetValue(code, out var text)
        ? usual
        : text.Trim().ToLowerInvariant() switch {
            "none" or "silent" => null,
            "error" => Severity.Error,
            "warning" => Severity.Warning,
            "suggestion" or "info" => Severity.Info,
            _ => usual,
        };
}
