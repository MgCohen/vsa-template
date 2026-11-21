namespace Umpire.Architecture;

internal sealed record KindStatement(string Rule, IReadOnlyList<Part> Subjects, TypeKind Kinds);
