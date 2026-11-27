namespace Umpire.CheckModel;

public sealed record TypeParameterFact(string Name, IReadOnlyList<string> Limits, IReadOnlyList<TypeShape> Types);
