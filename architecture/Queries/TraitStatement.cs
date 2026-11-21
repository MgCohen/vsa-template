namespace Umpire.Architecture;

internal sealed record TraitStatement(string Rule, IReadOnlyList<Part> Subjects, Trait Condition);
