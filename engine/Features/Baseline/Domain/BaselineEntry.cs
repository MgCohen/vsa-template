namespace Baseline.Domain;

public sealed record BaselineEntry(string Code, string File, string Message) {
    const char Separator = '|';

    public static BaselineEntry? Parse(string line) {
        var parts = line.Split(Separator, 3);
        return parts is [{ Length: > 0 } code, var file, var message] ? new BaselineEntry(code, file, message) : null;
    }

    public static BaselineEntry Of(string code, string? file, string message, string blockFolder) => new(
        code,
        file is null ? "" : RelativePath(blockFolder, file),
        message.ReplaceLineEndings(" "));

    public override string ToString() => $"{Code}{Separator}{File}{Separator}{Message}";

    static string RelativePath(string blockFolder, string file) =>
        Path.GetRelativePath(blockFolder, Path.GetFullPath(file)).Replace(Path.DirectorySeparatorChar, '/');
}
