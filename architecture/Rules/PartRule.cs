namespace VSA.Architecture;

public sealed record PartRule(Part From, Part To, string Condition, Permission Permission, string Source, string Text, Access Access = Access.Public);
