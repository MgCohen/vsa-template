namespace VSA.Architecture.Versions;

public static class VersionCheck {
    public const string Code = "VSA3001";

    public static IReadOnlyList<VersionSlot> Mismatches(IEnumerable<VersionSlot> slots, string expected) =>
        [.. slots.Where(slot => slot.Version != expected)];

    public static string Message(IReadOnlyList<VersionSlot> mismatches, string expected) =>
        $"The versions are not the same. Expected VSA {expected}, found: "
        + string.Join("; ", mismatches.Select(slot => $"{slot.File}: {slot.Package} {slot.Version ?? "(no version)"}"))
        + ". Use one version for the tool, VSA.Sdk and all VSA.* packages: run dotnet vsa update.";
}
