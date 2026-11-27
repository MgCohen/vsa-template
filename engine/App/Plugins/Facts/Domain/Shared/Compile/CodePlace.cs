namespace Facts.Domain.Shared.Compile;

public sealed record CodePlace(string File, int Line, int Column, int Length);
