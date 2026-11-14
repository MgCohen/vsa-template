namespace VSA.Architecture.Data;

public sealed record InstanceData(
    string Id, string Layer, bool Many, string? Parent, string Path, Dictionary<string, string> Items);
