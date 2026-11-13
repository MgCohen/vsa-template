namespace VSA.Architecture.Data;

public sealed record InstanceData(
    string Id, string Part, string Layer, string? Parent, string Path, Dictionary<string, string> Items);
