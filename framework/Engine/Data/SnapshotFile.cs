using System.Text.Json.Serialization;

namespace VSA.Architecture.Data;

public sealed record SnapshotFile(
    [property: JsonPropertyName("$schema")] string Schema,
    int Version,
    SnapshotArchitecture Architecture,
    AppData App);
