namespace Umpire.Architecture;

public sealed record PartRule(Part From, Part To, Condition Condition, Permission Permission, string Text, Access Access = Access.Public);
