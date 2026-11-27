namespace Umpire.Architecture;

internal sealed record MatchStatement(string Rule, IReadOnlyList<Part> Subjects, Type Taken, Type? Group);
