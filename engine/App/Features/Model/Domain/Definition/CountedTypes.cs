namespace Umpire.CheckModel;

public sealed record CountedTypes(
    bool InAnyPart, IReadOnlyList<int> Parts, TypeName? Concept = null, Depth? Depth = null, TypeName? Type = null);
