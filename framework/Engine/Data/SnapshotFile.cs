namespace VSA.Architecture.Data;

public sealed record SnapshotFile(int Version, SnapshotArchitecture Architecture, AppData App);
