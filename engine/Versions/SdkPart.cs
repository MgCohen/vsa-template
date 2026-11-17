namespace Umpire.Engine.Versions;

public sealed record SdkPart(string Name, string? Version) {
    public bool IsUmpire => VersionSlots.IsSdk(Name);

    public static SdkPart Parse(string part) =>
        part.Split('/', StringSplitOptions.TrimEntries) is [var name, var version]
            ? new SdkPart(name, version)
            : new SdkPart(part.Trim(), null);

    public static IReadOnlyList<SdkPart> AllOf(string? sdkAttribute) =>
        [.. (sdkAttribute ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(Parse)];

    public override string ToString() => Version is null ? Name : $"{Name}/{Version}";
}
