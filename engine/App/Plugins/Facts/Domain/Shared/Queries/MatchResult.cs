namespace Umpire.Architecture;

public sealed record MatchResult(string Rule, Binding Subject, Binding? Taken, string Message);
