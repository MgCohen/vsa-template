namespace VSA.Architecture.Engine;

public sealed record Source(string File, int Line, int Column = 1);
