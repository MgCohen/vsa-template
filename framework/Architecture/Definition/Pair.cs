namespace VSA.Architecture;

public sealed record Pair(Part From, Part To, string Condition, Permission Permission, string Source, string Rule, Access Access = Access.Public);
