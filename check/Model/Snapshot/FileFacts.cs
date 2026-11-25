namespace Umpire.CheckModel;

public sealed record FileFacts(string Path, string Hash, long Modified, IReadOnlyList<TypeFacts> Types);
