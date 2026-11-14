namespace VSA.Architecture.Data;

public sealed record PartData(
    string Id, string Layer, string? Parent, string Path, bool Many, bool HasFolder, List<string> OfType);
