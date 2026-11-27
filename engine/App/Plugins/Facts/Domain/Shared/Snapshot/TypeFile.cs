namespace Umpire.CheckModel;

public sealed record TypeFile(string File, TypeFacts Facts, bool IsGenerated = false);
