namespace Umpire.Engine.Versions;

public static class VersionCheck {
    public const string Code = "UMP030201";

    public static IReadOnlyList<VersionSlot> Mismatches(IEnumerable<VersionSlot> slots, string expected) =>
        [.. slots.Where(slot => slot.IsChecked && slot.Version != expected)];

    public static string Message(IReadOnlyList<VersionSlot> mismatches, string expected) =>
        $"The versions are not the same. Expected Umpire {expected}, found: "
        + string.Join("; ", mismatches.Select(Found))
        + ". Use one version for the tool, Umpire.Sdk and all Umpire.* packages: run dotnet umpire update.";

    public static string Shown(VersionSlot slot) => slot switch {
        { Version: null } => "(no version)",
        { IsChecked: false } => $"(not checked: {slot.Text})",
        _ => slot.Version,
    };

    static string Found(VersionSlot slot) => slot.Version is null
        ? $"{slot.File}: {slot.Package} has no version; add Version or a central PackageVersion"
        : $"{slot.File}: {slot.Package} {slot.Version}";
}
