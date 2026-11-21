namespace Umpire.Architecture;

internal sealed record OwnerStatement(string Rule, IReadOnlyList<Part> Subjects, Type Attribute);
