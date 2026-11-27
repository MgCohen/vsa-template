namespace Umpire.Architecture;

internal sealed record PlaceStatement(string Rule, IReadOnlyList<Part> Places, Type Type);
