namespace Umpire.CheckModel;

public sealed record ParameterFact(string Name, TypeShape Type, bool IsOptional, bool IsParams, string? Passing = null);
