namespace Umpire.CheckModel;

public sealed record Finding(string Code, Severity Severity, string Text, string? File = null, int Line = 1, int Column = 1) {
    const string Origin = "umpire-engine";

    public override string ToString() {
        var origin = File is null ? Origin : $"{File}({Line},{Column})";
        return Severity == Severity.Info
            ? $"{origin}: {Code}: {Text}"
            : $"{origin}: {(Severity == Severity.Error ? "error" : "warning")} {Code}: {Text}";
    }
}
