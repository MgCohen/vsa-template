namespace Umpire.Config;

public sealed class ConfigFormatException : FormatException {
    public ConfigFormatException(string sourceName, int line, int column, string reason)
        : base($"{sourceName}({line},{column}): {reason}") {
        SourceName = sourceName;
        Line = line;
        Column = column;
        Reason = reason;
    }

    public string SourceName { get; }

    public int Line { get; }

    public int Column { get; }

    public string Reason { get; }
}
