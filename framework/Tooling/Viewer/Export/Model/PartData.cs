namespace Viewer.Export.Model;

public sealed record PartData(
    string Id, string Layer, string? Parent, string Path, bool Many, bool HasFolder, List<string> OfType);
