namespace VSA.Architecture.Data;

public static class DataFiles {
    public const string ArchitectureExtension = ".architecture.json";
    public const string SnapshotExtension = ".snapshot.json";

    public static string Architecture(string name) => name.ToLowerInvariant() + ArchitectureExtension;

    public static string Snapshot(string name) => name.ToLowerInvariant() + SnapshotExtension;
}
