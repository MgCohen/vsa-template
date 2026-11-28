namespace Umpire.Kernels;

public sealed record Operation(string Name, IReadOnlyDictionary<string, string?> Values);
