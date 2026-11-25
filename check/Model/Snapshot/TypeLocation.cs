namespace Umpire.CheckModel;

public sealed record TypeLocation(string Namespace, int Line, int Column, string? DeclaringType = null);
