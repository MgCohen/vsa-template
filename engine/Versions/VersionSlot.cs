namespace Umpire.Engine.Versions;

public sealed class VersionSlot(
    string file, string package, string? text, int? line = null, Action<string>? write = null) {
    const string PropertyStart = "$(";

    public string File => file;

    public string Package => package;

    public string? Text { get; private set; } = text;

    public string? Version => Text is string value && IsExactRange(value) ? value.Substring(1, value.Length - 2).Trim() : Text;

    public bool IsChecked => Text is null || !Text.Contains(PropertyStart, StringComparison.Ordinal);

    public int? Line => line;

    public bool Set(string version) {
        if (write is null || Text is null || !IsChecked || Version == version) {
            return false;
        }
        var value = IsExactRange(Text) ? $"[{version}]" : version;
        write(value);
        Text = value;
        return true;
    }

    static bool IsExactRange(string value) =>
        value.StartsWith('[') && value.EndsWith(']') && !value.Contains(',');
}
