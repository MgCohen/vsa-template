namespace Umpire.CheckModel;

public sealed record FileFacts(string Path, string Hash, long Modified, bool IsGenerated, IReadOnlyList<TypeFacts> Types);
