namespace Umpire.Architecture;

public sealed record CountResult(string Rule, CountProblem Problem, string Subject, IReadOnlyList<Binding> Found, Binding At, string Message);
