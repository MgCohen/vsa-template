namespace Umpire.CheckModel;

public sealed record NameRule(string Start, string End, bool OfAGroup, TypeName? Group = null, int? GroupPart = null);
