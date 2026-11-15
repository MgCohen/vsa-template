namespace VSA.Architecture;

public sealed record Source(string File, int Line, int Column = 1);
