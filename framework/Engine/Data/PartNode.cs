namespace VSA.Architecture.Data;

public sealed record PartNode(
    string Concept,
    bool Group,
    string? Folder,
    Depth? Depth,
    Access? Access,
    Boundary? Boundary,
    List<string> Types,
    List<PartNode> Children);
