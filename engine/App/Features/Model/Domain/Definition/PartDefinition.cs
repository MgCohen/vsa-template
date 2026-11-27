namespace Umpire.CheckModel;

public sealed record PartDefinition(
    TypeName Concept,
    string BodyFolder,
    bool IsGroup,
    bool IsBlockPlace,
    bool IsKernelRoot,
    bool IsBlock,
    bool IsApp,
    PartOptions Options,
    IReadOnlyList<TypeName> Types,
    IReadOnlyList<PartDefinition> Children,
    string? Folder = null,
    string? External = null,
    NameRule? Name = null);
